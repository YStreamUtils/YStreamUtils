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
using YStreamUtils.Core.Models;
using YStreamUtils.SDK.Plugin;
using YStreamUtils.SDK.Registry;

namespace YStreamUtils.Core.Services;

public class PluginService
{
    private readonly string _pluginDir = Path.Combine(Consts.ApplicationDataFolder, "plugins");
    private readonly SemaphoreSlim _lock = new(1, 1);

    private ServiceProvider _activePlugins = new ServiceCollection().BuildServiceProvider();
    private readonly Dictionary<string, Plugin> _loadedPlugins = new();
    private readonly List<AssemblyLoadContext> _pluginContexts = [];
    private readonly ILogger<PluginService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HttpClient _httpClient;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly SettingsService _settingsService;
    private readonly IServiceProvider _hostProvider;

    public PluginService(ILogger<PluginService> logger,
        ILoggerFactory loggerFactory,
        HttpClient httpClient,
        IDbContextFactory<AppDbContext> contextFactory,
        SettingsService settingsService,
        IServiceProvider hostProvider)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _httpClient = httpClient;
        _contextFactory = contextFactory;
        _settingsService = settingsService;
        _hostProvider = hostProvider;
        
        AssemblyLoadContext.Default.Resolving += (_, assemblyName) =>
        {
            return _pluginContexts
                .SelectMany(ctx => ctx.Assemblies)
                .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
        };

        _ = ReloadLocalPluginsAsync();
    }


    public async Task DownloadAndInstallPluginAsync(PluginManifest manifest)
    {
        var version = manifest.Version.StartsWith('v') ? manifest.Version : "v" + manifest.Version;
        var downloadUrl =
            $"https://github.com/{manifest.Source.Owner}/{manifest.Source.Repository}/releases/download/{version}/{manifest.Name}.dll";

        _logger.LogInformation("Streaming raw plugin binary file target asset: {Url}", downloadUrl);

        using var response = await _httpClient.GetAsync(downloadUrl);
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

public async Task ReloadLocalPluginsAsync()
{
    await _lock.WaitAsync();
    try
    {
        _logger.LogInformation("Before Lock 2");
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
                _logger.LogError("No files found for folder: {FolderPath}", folderPath);
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
                _logger.LogInformation("Loading primary assembly for: {Name}", primaryDll);
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
                    throw new UnauthorizedAccessException($"No assembly in '{folderName}' has [assembly: PluginManifestAttribute].");

                var manifest = attr.ToManifest();

                var schemaNode = attr.SettingsType is not null
                    ? Consts.IndentedSerializerOptions.GetJsonSchemaAsNode(attr.SettingsType)
                    : new JsonObject();

                var entrypointType = pluginAssembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

                if (entrypointType == null)
                    throw new InvalidDataException($"Entrypoint class implementing IPlugin missing inside {manifest.Name}.dll");
                
                var genericLoggerType = typeof(Logger<>).MakeGenericType(entrypointType);
                var pluginLogger = Activator.CreateInstance(genericLoggerType, _loggerFactory)!;
                
                var pluginInstance = (IPlugin)ActivatorUtilities.CreateInstance(_hostProvider, entrypointType, pluginLogger);

                pluginServiceCollection.AddSingleton(entrypointType, pluginInstance);
                _loadedPlugins.Add(manifest.Name, new Plugin(entrypointType, manifest, schemaNode));
                _pluginContexts.Add(loadContext);

                _logger.LogInformation("Successfully instantiated Plugin: {Name}", manifest.Name);
            }
            catch (Exception ex)
            {
                loadContext?.Unload();
                _logger.LogError(ex, "Failed execution configuration for folder: {Dir}", folderName);
            }
        }
        
        _activePlugins = pluginServiceCollection.BuildServiceProvider();
    }
    finally
    {
        _lock.Release();
        var lsp = _hostProvider.GetRequiredService<RoslynLspProvider>();
        lsp.InvalidateCache();
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
        
        using var dbContext = _contextFactory.CreateDbContext();
        
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

        using var dbContext = _contextFactory.CreateDbContext();

        var pluginSettings = dbContext.PluginSettings.First(x => x.PluginName == plugin.Manifest.Name);
        pluginSettings.SettingsJson = newSettings;
        dbContext.SaveChanges();
    }

    public async Task<List<PluginManifest>> FetchAllRegistryPluginsAsync()
    {
        var registryUrls = _settingsService.GetSettings().PluginSettings.Repositories;
        if (registryUrls == null || registryUrls.Count == 0)
        {
            throw new InvalidOperationException("No plugin registry repository endpoints are configured in settings.");
        }

        var unifiedPlugins = new List<PluginManifest>();
        var seenPlugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in registryUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
        {
            _logger.LogInformation("Querying plugin registry workspace source: {Url}", url);
            try
            {
                var plugins = await FetchSingleRegistryAsync(url);
                unifiedPlugins.AddRange(plugins.Where(plugin => seenPlugins.Add(plugin.Name)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skipping failed registry destination pipeline context: {Url}", url);
            }
        }

        return unifiedPlugins;
    }

    private async Task<List<PluginManifest>> FetchSingleRegistryAsync(string url)
    {
        var distribution = await _httpClient.GetFromJsonAsync<RegistryDistribution>(url);
        return distribution?.Plugins ?? [];
    }
}