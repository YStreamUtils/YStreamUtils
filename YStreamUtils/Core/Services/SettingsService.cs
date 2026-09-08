using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Services;

public partial class SettingsService
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
            Repositories = ["https://ystreamutils.github.io/YStreamUtils-Plugin-Registry/registry.toml"]
        }
    };

    public SettingsService(ILogger<SettingsService> logger, string baseDirectoryPath)
    {
        _logger = logger;
        _settingsPath = Path.Combine(baseDirectoryPath, "settings.json");
        _settings = new Settings();

        try
        {
            LoadSettings();
        }
        catch (Exception)
        {
            _settings = DefaultSettings;
            SaveSettings(_settings);
        }
    }

    public void LoadSettings()
    {
        _logger.LogInformation("Trying to load settings configuration from file system at {Path}", _settingsPath);

        if (!File.Exists(_settingsPath))
        {
            throw new FileNotFoundException("Configuration target missing.");
        }

        try
        {
            string jsonContent = File.ReadAllText(_settingsPath);

            // 🚀 FIX: Pass the generated static default type metadata context
            var loadedSettings = JsonSerializer.Deserialize(
                jsonContent,
                AppJsonSerializerContext.Default.Settings
            );

            _settings = loadedSettings ?? DefaultSettings;
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

            var options = new JsonSerializerOptions { WriteIndented = true };
            var context = new AppJsonSerializerContext(options);
            var jsonContent = JsonSerializer.Serialize(settings, context.Settings);

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

public static class SettingsServiceEndpointsExtension
{
    public static RouteGroupBuilder AddSettingsService(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("/settings",
                ([FromServices] SettingsService settingsService) => { Results.Ok(settingsService.GetSettings()); })
            .Produces<Settings>()
            .WithName("GetSettings");

        groupBuilder.MapPost("/settings", (Settings settings, [FromServices] SettingsService settingsService) =>
            {
                settingsService.SaveSettings(settings);
                return Results.Ok(settingsService.GetSettings());
            })
            .Produces<Settings>()
            .WithName("SaveSettings");
        return groupBuilder;
    }
}