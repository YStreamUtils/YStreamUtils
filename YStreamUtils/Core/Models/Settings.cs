using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Models;

public class Settings
{
    [JsonPropertyName("ui")]
    public UiSettings UiSettings { get; set; } = new();

    [JsonPropertyName("plugins")]
    public PluginSettings PluginSettings { get; set; } = new();
}

public class UiSettings
{
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "dark";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#9900ff";

    [JsonPropertyName("fullyCloseSidebar")]
    public bool FullyCloseSidebar { get; set; } = false;
}

public class PluginSettings
{
    [JsonPropertyName("repos")]
    public List<string> Repositories { get; set; } =
        ["https://ystreamutils.github.io/YStreamUtils-Plugin-Registry/registry.toml"];
}