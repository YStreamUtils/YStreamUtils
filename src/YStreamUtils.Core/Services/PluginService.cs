using System.IO.Compression;
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
using YScriptEngine.Lsp.Roslyn;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.SDK.Plugin;
using YStreamUtils.SDK.Registry;
using PluginSettings = YStreamUtils.Core.Entities.PluginSettings;

namespace YStreamUtils.Core.Services;

public class PluginService(
    ILogger<PluginService> logger,
    ILoggerFactory loggerFactory,
    HttpClient httpClient,
    IDbContextFactory<AppDbContext> contextFactory,
    SettingsService settingsService,
    IEventBus eventBus,
    IServiceProvider hostProvider)
{
    private readonly string _pluginDir = Path.Combine(Consts.ApplicationDataFolder, "plugins");
    private readonly SemaphoreSlim _lock = new(1, 1);

    private ServiceProvider _activePlugins = new ServiceCollection().BuildServiceProvider();
    private readonly Dictionary<string, Plugin> _loadedPlugins = new();
    private readonly List<AssemblyLoadContext> _pluginContexts = [];


    public async Task DownloadAndInstallPluginAsync(PluginManifest manifest)
    {
        var version = manifest.Version.StartsWith('v') ? manifest.Version : "v" + manifest.Version;
        var downloadUrl =
            $"https://github.com/{manifest.Source.Owner}/{manifest.Source.Repository}/releases/download/{version}/{manifest.Name}.zip";

        logger.LogInformation("Streaming plugin binary file: {Url}", downloadUrl);

        using var response = await httpClient.GetAsync(downloadUrl);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP bundle asset download failed status: {response.StatusCode}");
        }

        var targetDir = Path.Combine(_pluginDir, manifest.Name);
        Directory.CreateDirectory(targetDir);

        await using (var zipStream = await response.Content.ReadAsStreamAsync())
        {
            await ZipFile.ExtractToDirectoryAsync(zipStream, targetDir, overwriteFiles: true);
        }

        var manifestPath = Path.Combine(targetDir, "manifest.json");
        var jsonText = JsonSerializer.Serialize(manifest, Consts.IndentedSerializerOptions);
        await File.WriteAllTextAsync(manifestPath, jsonText, Encoding.UTF8);

        await ReloadLocalPluginsAsync();
    }

    public async Task ReloadLocalPluginsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var oldContext in _pluginContexts)
            {
                oldContext.Unload();
            }

            _pluginContexts.Clear();
            _loadedPlugins.Clear();

            Directory.CreateDirectory(_pluginDir);
            var directories = Directory.GetDirectories(_pluginDir);
            var pluginServiceCollection = new ServiceCollection();

            foreach (var folderPath in directories)
            {
                var folderName = Path.GetFileName(folderPath);
                var sharedDllFileName = Path.GetFileName(typeof(PluginManifestAttribute).Assembly.Location);
                var dllFiles = Directory.GetFiles(folderPath, "*.dll")
                    .Where(f =>
                    {
                        var fileName = Path.GetFileName(f);
                        return !fileName.Equals(sharedDllFileName, StringComparison.OrdinalIgnoreCase)
                               && !fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
                               && !fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase);
                    })
                    .ToArray();

                if (dllFiles.Length == 0)
                {
                    logger.LogError("No files found for folder: {FolderPath}", folderPath);
                    continue;
                }

                var expectedDll = Path.Combine(folderPath, $"{folderName}.dll");
                var orderedDlls = File.Exists(expectedDll)
                    ? dllFiles.OrderByDescending(f => f.Equals(expectedDll, StringComparison.OrdinalIgnoreCase))
                    : dllFiles.AsEnumerable();

                PluginLoadContext? loadContext = null;

                try
                {
                    var enumerable = orderedDlls as string[] ?? [.. orderedDlls];
                    var primaryDll = enumerable.First();
                    logger.LogInformation("Loading primary assembly for: {Name}", primaryDll);
                    loadContext = new PluginLoadContext(folderName, primaryDll);

                    Assembly? pluginAssembly = null;
                    PluginManifestAttribute? attr = null;
                    
                    foreach (var dll in enumerable)
                    {
                        var candidate = loadContext.LoadFromAssemblyPath(Path.GetFullPath(dll));
                        var candidateAttr = candidate.GetCustomAttribute<PluginManifestAttribute>();

                        if (candidateAttr == null) continue;
                        pluginAssembly = candidate;
                        attr = candidateAttr;
                        break;
                    }

                    if (pluginAssembly == null || attr == null)
                        throw new UnauthorizedAccessException(
                            $"No assembly in '{folderName}' has [assembly: PluginManifestAttribute].");

                    var manifest = attr.ToManifest();

                    var schemaNode = attr.SettingsType is not null
                        ? Consts.IndentedSerializerOptions.GetJsonSchemaAsNode(attr.SettingsType)
                        : new JsonObject();

                    var entrypointType = pluginAssembly.GetTypes()
                        .FirstOrDefault(t =>
                            typeof(IPlugin).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

                    if (entrypointType == null)
                        throw new InvalidDataException(
                            $"Entrypoint class implementing IPlugin missing inside {manifest.Name}.dll");
                    
                    var pluginInstance =
                        (IPlugin)ActivatorUtilities.CreateInstance(hostProvider, entrypointType);
                    
                    logger.LogInformation("Plugin {Plugin} instantiated, Initializing...", manifest.Name);
                    await pluginInstance.InitializeAsync();

                    var dbContext = await contextFactory.CreateDbContextAsync();
                    var existingSettings = await dbContext.PluginSettings.FirstOrDefaultAsync(ps =>
                        ps.PluginName == manifest.Name);
                    if (existingSettings is not null)
                    {
                        logger.LogInformation("Existing settings found for {Plugin}, applying.", manifest.Name);
                        _ = pluginInstance.SettingsUpdatedAsync(existingSettings.SettingsJson);
                    }
                    else
                    {
                        if (attr.SettingsType is not null)
                        {
                            logger.LogInformation("No settings found for {Plugin}, creating from default", manifest.Name);
                            var defaultSettings = Activator.CreateInstance(attr.SettingsType);
                            var serialized =
                                JsonSerializer.Serialize(defaultSettings, Consts.IndentedSerializerOptions);
                            _ = pluginInstance.SettingsUpdatedAsync(serialized);
                            dbContext.PluginSettings.Add(new PluginSettings
                            {
                                PluginName = manifest.Name,
                                SettingsJson = serialized
                            });
                            await dbContext.SaveChangesAsync();
                        }
                    }


                    pluginServiceCollection.AddSingleton(entrypointType, pluginInstance);
                    _loadedPlugins.Add(manifest.Name, new Plugin(entrypointType, manifest, schemaNode));
                    _pluginContexts.Add(loadContext);

                    logger.LogInformation("Successfully instantiated Plugin: {Name}", manifest.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed execution configuration for folder: {Dir}", folderName);
                    loadContext?.Unload();
                }
            }

            _activePlugins = pluginServiceCollection.BuildServiceProvider();
        }
        finally
        {
            _lock.Release();
            var lsp = hostProvider.GetRequiredService<RoslynLspProvider>();
            lsp.InvalidateCache();
            await eventBus.PublishAsync(EventKey.PluginsChanged, new EmptyStruct());
        }
    }

    public IServiceProvider GetPluginServiceProvider() => _activePlugins;

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