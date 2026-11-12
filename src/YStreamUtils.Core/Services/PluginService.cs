using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Json.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Models;
using YStreamUtils.SDK.Plugin;
using YStreamUtils.SDK.Registry;

namespace YStreamUtils.Core.Services;

public class PluginService(
    ILogger<PluginService> logger,
    HttpClient httpClient,
    IDbContextFactory<AppDbContext> contextFactory,
    SettingsService settingsService,
    IServiceProvider hostProvider)
{
    private readonly string _pluginDir = Path.Combine(Consts.ApplicationDataFolder, "plugins");
    private readonly SemaphoreSlim _lock = new(1, 1);

    private ServiceProvider _activePlugins = new ServiceCollection().BuildServiceProvider();
    private readonly Dictionary<string, Plugin> _loadedPlugins = new();

    public async Task DownloadAndInstallPluginAsync(PluginManifest manifest)
    {
        var version = manifest.Version.StartsWith('v') ? manifest.Version : "v" + manifest.Version;
        var downloadUrl =
            $"https://github.com/{manifest.Source.Owner}/{manifest.Source.Repository}/releases/download/{version}/{manifest.Name}.dll";

        logger.LogInformation("Streaming raw plugin binary file target asset: {Url}", downloadUrl);

        using var response = await httpClient.GetAsync(downloadUrl);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP bundle asset download failed status: {response.StatusCode}");
        }

        var targetDir = Path.Combine(_pluginDir, manifest.Name);
        Directory.CreateDirectory(targetDir);

        var dllFilePath = Path.Combine(targetDir, $"{manifest.Name}.dll");

        await using (var byteStream = await response.Content.ReadAsStreamAsync())
        await using (var fileStream = new FileStream(dllFilePath, FileMode.Create, FileAccess.Write, FileShare.None,
                         4096, useAsync: true))
        {
            await byteStream.CopyToAsync(fileStream);
        }

        var manifestPath = Path.Combine(targetDir, "manifest.json");
        var jsonText = JsonSerializer.Serialize(manifest, Consts.IndentedSerializerOptions);
        await File.WriteAllTextAsync(manifestPath, jsonText, Encoding.UTF8);

        await ReloadLocalPluginsAsync();
    }

private async Task ReloadLocalPluginsAsync()
{
    await _lock.WaitAsync();
    try
    {
        Directory.CreateDirectory(_pluginDir);
        var directories = Directory.GetDirectories(_pluginDir);
        var pluginServiceCollection = new ServiceCollection();
        _loadedPlugins.Clear();
        
        foreach (var folderPath in directories)
        {
            var folderName = Path.GetDirectoryName(folderPath);
            var dllFiles = Directory.GetFiles(folderPath, "*.dll");
            
            switch (dllFiles.Length)
            {
                case 0:
                    logger.LogError("No files found for folder: {FolderPath}", folderPath);
                    continue;
                case > 1:
                    logger.LogError("Multiple files found for folder: {FolderPath}", folderPath);
                    continue;
            }

            var dllPath = dllFiles[0];

            try
            {
                var loadContext = new AssemblyLoadContext($"ALC_{folderName}", isCollectible: false);

                await using var dllStream = File.OpenRead(dllPath);
                var assembly = loadContext.LoadFromStream(dllStream);

                var attr = assembly.GetCustomAttribute<PluginManifestAttribute>();
                if (attr == null)
                    throw new UnauthorizedAccessException($"Assembly '{Path.GetFileName(dllPath)}' is missing [assembly: PluginManifestAttribute].");

                var manifest = attr.ToManifest();

                var schemaNode = attr.SettingsType is not null
                    ? Consts.IndentedSerializerOptions.GetJsonSchemaAsNode(attr.SettingsType)
                    : new JsonObject();

                var entrypointType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

                if (entrypointType == null)
                    throw new InvalidDataException($"Entrypoint class implementing IPlugin missing inside {manifest.Name}.dll");

                var pluginInstance = (IPlugin)ActivatorUtilities.CreateInstance(hostProvider, entrypointType);

                pluginServiceCollection.AddSingleton(pluginInstance);
                _loadedPlugins.Add(manifest.Name, new Plugin(manifest, schemaNode));


                logger.LogInformation("Successfully instantiated C# Plugin Engine Environment context for: {Name}", manifest.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed execution configuration for folder: {Dir}", folderName);
            }
        }

        _activePlugins = pluginServiceCollection.BuildServiceProvider();
    }
    finally
    {
        _lock.Release();
    }
}

    public Dictionary<string, Plugin> GetActivePlugins()
    {
        _lock.Wait();
        try
        {
            return new Dictionary<string, Plugin>(_loadedPlugins);
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public string GetSettingsForPlugin(string name)
    {
        _loadedPlugins.TryGetValue(name, out var plugin);
        if (plugin is null)
        {
            throw new KeyNotFoundException($"Plugin {name} not found");
        }
        
        using var dbContext = contextFactory.CreateDbContext();
        
        var pluginSettings = dbContext.PluginSettings.FirstOrDefault(x => x.PluginName == plugin.Manifest.Name);
        return pluginSettings?.SettingsJson ?? "{}";
    }

    public string GetSettingsSchemaForPlugin(string name)
    {
        _loadedPlugins.TryGetValue(name, out var plugin);
        return plugin is null 
            ? throw new KeyNotFoundException($"Plugin {name} not found") 
            : plugin.PluginSettingsSchema.ToJsonString();
    }

    public void SetSettingsForPlugin(string name, string newSettings)
    {
        _loadedPlugins.TryGetValue(name, out var plugin);
        if (plugin is null)
        {
            throw new KeyNotFoundException($"Plugin {name} not found");
        }

        var schema = JsonSchema.FromText(plugin.PluginSettingsSchema.ToJsonString());

        using var jsonDocument = JsonDocument.Parse(newSettings);

        var result = schema.Evaluate(jsonDocument.RootElement);
        if (!result.IsValid)
        {
            var errors = string.Join("; ", result.Errors?.Select(e => $"{e.Key}: {e.Value}") ?? []);
            throw new ArgumentException($"Invalid plugin settings configuration. Errors: {errors}");
        }

        using var dbContext = contextFactory.CreateDbContext();

        var pluginSettings = dbContext.PluginSettings.First(x => x.PluginName == plugin.Manifest.Name);
        pluginSettings.SettingsJson = newSettings;
        dbContext.SaveChanges();
    }

    public async Task<List<PluginManifest>> FetchAllRegistryPluginsAsync()
    {
        var registryUrls = settingsService.GetSettings().PluginSettings.Repositories;
        if (registryUrls == null || registryUrls.Count == 0)
        {
            throw new InvalidOperationException("No plugin registry repository endpoints are configured in settings.");
        }

        var unifiedPlugins = new List<PluginManifest>();
        var seenPlugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in registryUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
        {
            logger.LogInformation("Querying plugin registry workspace source: {Url}", url);
            try
            {
                var plugins = await FetchSingleRegistryAsync(url);
                unifiedPlugins.AddRange(plugins.Where(plugin => seenPlugins.Add(plugin.Name)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Skipping failed registry destination pipeline context: {Url}", url);
            }
        }

        return unifiedPlugins;
    }

    private async Task<List<PluginManifest>> FetchSingleRegistryAsync(string url)
    {
        var distribution = await httpClient.GetFromJsonAsync<RegistryDistribution>(url);
        return distribution?.Plugins ?? [];
    }
}