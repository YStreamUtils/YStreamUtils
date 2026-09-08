using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services;

public class PluginService(ILogger<PluginService> logger, SettingsService settings, HttpClient httpClient)
{
    private readonly string _pluginDir = Path.Combine(Consts.ApplicationDataFolder, "plugins");
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    private Dictionary<string, Plugin> _activePlugins = new(StringComparer.OrdinalIgnoreCase);
    private string _pluginTypeCache = string.Empty;

    public async Task<List<PluginManifest>> FetchAllRegistryPluginsAsync()
    {
        var registryUrls = settings.GetSettings().PluginSettings.Repositories;
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

    public async Task DownloadAndInstallPluginAsync(PluginManifest manifest)
    {
        if (string.IsNullOrEmpty(manifest.Name) || string.IsNullOrEmpty(manifest.EntryPoint) ||
            string.IsNullOrEmpty(manifest.Source.Owner) || string.IsNullOrEmpty(manifest.Source.Repository) ||
            string.IsNullOrEmpty(manifest.Version))
        {
            throw new ArgumentException("Cannot process manifest: missing explicit identification or version metadata properties.");
        }

        var version = manifest.Version.StartsWith('v') ? manifest.Version : "v" + manifest.Version;
        var downloadUrl = $"https://github.com/{manifest.Source.Owner}/{manifest.Source.Repository}/releases/download/{version}/{manifest.Name}.zip";

        logger.LogInformation("Streaming structured zip distribution target archive: {Url}", downloadUrl);

        using var response = await httpClient.GetAsync(downloadUrl);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP bundle asset download failed status: {response.StatusCode}");
        }

        var targetDir = Path.Combine(_pluginDir, manifest.Name);
        Directory.CreateDirectory(targetDir);

        await using var byteStream = await response.Content.ReadAsStreamAsync();
        await using var archive = new ZipArchive(byteStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            var parts = entry.FullName.Split('/', 2);
            var relativePath = (parts.Length == 2 && parts[0] == manifest.Name) ? parts[1] : entry.FullName;

            if (string.Equals(relativePath, "manifest.json", StringComparison.OrdinalIgnoreCase))
            {
                continue; 
            }

            var filePath = Path.Combine(targetDir, relativePath);

            if (string.IsNullOrEmpty(entry.Name)) 
            {
                Directory.CreateDirectory(filePath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await entry.ExtractToFileAsync(filePath, overwrite: true);
        }

        var manifestPath = Path.Combine(targetDir, "manifest.json");
        var jsonText = JsonSerializer.Serialize(manifest);
        await File.WriteAllTextAsync(manifestPath, jsonText, Encoding.UTF8);

        await ReloadLocalPluginsAsync();
    }

    public Dictionary<string, Plugin> GetActivePlugins()
    {
        _lock.Wait();
        try
        {
            return new Dictionary<string, Plugin>(_activePlugins);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ReloadLocalPluginsAsync()
{
    await _lock.WaitAsync();
    try
    {
        Directory.CreateDirectory(_pluginDir);
        var directories = Directory.GetDirectories(_pluginDir);
        var newActivePlugins = new Dictionary<string, Plugin>(StringComparer.OrdinalIgnoreCase);

        foreach (var folderPath in directories)
        {
            var folderName = Path.GetFileName(folderPath);
            var manifestPath = Path.Combine(folderPath, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                logger.LogWarning("Failed to locate manifest.json inside directory: {Dir}", folderName);
                continue;
            }

            try
            {
                var jsonText = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(jsonText) 
                               ?? throw new InvalidDataException();

                logger.LogInformation("Found plugin {Name} at {Path}", manifest.Name, manifestPath);
                var pluginNamespace = GetSafePluginNamespace(manifest.Name);
                
                var fullEntryPointPath = Path.Combine(folderPath, manifest.EntryPoint);
                if (!File.Exists(fullEntryPointPath))
                {
                    logger.LogError("EntryPoint file missing for plugin {Name}: {Path}", manifest.Name, fullEntryPointPath);
                    continue;
                }
                var bundledJs = await File.ReadAllTextAsync(fullEntryPointPath, Encoding.UTF8);

                var typeDefs = string.Empty;
                var defFilePath = Path.Combine(folderPath, "index.d.ts");
                if (File.Exists(defFilePath))
                {
                    typeDefs = await File.ReadAllTextAsync(defFilePath);
                }

                newActivePlugins[pluginNamespace] = new Plugin(bundledJs, typeDefs, manifest);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed decoding manifest.json or loading pre-bundled script for directory: {Dir}", folderName);
            }
        }

        _pluginTypeCache = BuildDynamicPluginDefinitions(newActivePlugins);
        _activePlugins = newActivePlugins;
    }
    finally
    {
        _lock.Release();
    }
}

    private static string GetSafePluginNamespace(string input) => 
        new string(input.Where(char.IsLetterOrDigit).ToArray());

    private static string BuildDynamicPluginDefinitions(Dictionary<string, Plugin> plugins) => 
        string.Join("\n", plugins.Values.Select(p => p.TypeScriptDefs));

    public string GetDynamicPluginDefinitions() => _pluginTypeCache;
}
