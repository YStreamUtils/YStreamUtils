using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Models;


[JsonSerializable(typeof(ApplicationPlatform))]
[JsonSerializable(typeof(Platform))]

[JsonSerializable(typeof(Plugin))]
[JsonSerializable(typeof(PluginManifest))]
[JsonSerializable(typeof(SourceConfig))]
[JsonSerializable(typeof(DocumentationConfig))]
[JsonSerializable(typeof(RegistryDistribution))]


[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(UiSettings))]
[JsonSerializable(typeof(PluginSettings))]
public partial class ModelJsonContext : JsonSerializerContext
{
}
