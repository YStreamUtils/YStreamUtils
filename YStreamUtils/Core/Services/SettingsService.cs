using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services;

public class SettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsPath;
    private Settings _settings;

    private static Settings DefaultSettings => new()
    {
        UiSettings = new UiSettings
        {
            Theme = "dark",
            Color = "#9900ff",
            FullyCloseSidebar = false
        },
        PluginSettings = new PluginSettings
        {
            Repositories = ["https://ystreamutils.github.io/YStreamUtils-Plugin-Registry/registry.json"]
        }
    };

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _settingsPath = Path.Combine(Consts.ApplicationDataFolder, "settings.json");

        try
        {
            _settings = LoadSettings();
        }
        catch (Exception)
        {
            _settings = DefaultSettings;
            SaveSettings(_settings);
        }
    }

    private Settings LoadSettings()
    {
        _logger.LogInformation("Trying to load settings configuration from file system at {Path}", _settingsPath);

        if (!File.Exists(_settingsPath))
        {
            throw new FileNotFoundException("Configuration target missing.");
        }

        try
        {
            var jsonContent = File.ReadAllText(_settingsPath);

            var loadedSettings = JsonSerializer.Deserialize<Settings>(jsonContent);

            return loadedSettings ?? DefaultSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decode configuration file path target");
            throw;
        }
    }

    public void SaveSettings(Settings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            
            var jsonContent = JsonSerializer.Serialize(settings, Consts.IndentedSerializerOptions);

            File.WriteAllText(_settingsPath, jsonContent);
            _settings = settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration targets to disk");
            throw;
        }
    }

    public Settings GetSettings()
    {
        return _settings;
    }
}