using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace YStreamUtils.Core.Models;

public record Plugin(string JavaScriptCode, string TypeScriptDefs, PluginManifest Manifest);

public class PluginManifest
{
    [Required]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [Required]
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [Required]
    [JsonPropertyName("entryPoint")]
    public required string EntryPoint { get; init; }

    [Required]
    [JsonPropertyName("permissions")]
    public required List<string> Permissions { get; init; }

    [Required]
    [JsonPropertyName("authors")]
    public required List<string> Authors { get; init; }

    [Required]
    [JsonPropertyName("source")]
    public required SourceConfig Source { get; init; }

    [Required]
    [JsonPropertyName("documentation")]
    public required DocumentationConfig Documentation { get; init; }
}

[UsedImplicitly]
public class SourceConfig
{
    [Required]
    [JsonPropertyName("repository")]
    public required string Repository { get; init; }

    [Required]
    [JsonPropertyName("owner")]
    public required string Owner { get; init; }
}

[UsedImplicitly]
public class DocumentationConfig
{
    [Required]
    [JsonPropertyName("description")]
    public required string Description { get; init; }
}

public class RegistryDistribution
{
    public List<PluginManifest> Plugins { get; init; } = [];
}
