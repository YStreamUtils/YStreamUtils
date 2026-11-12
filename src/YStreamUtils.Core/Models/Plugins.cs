using System.Text.Json.Nodes;
using YStreamUtils.SDK.Plugin;

namespace YStreamUtils.Core.Models;

public record Plugin(Type PluginType, PluginManifest Manifest, JsonNode PluginSettingsSchema);
