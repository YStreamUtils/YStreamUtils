using System.Text.Json.Nodes;
using YStreamUtils.SDK.Plugin;

namespace YStreamUtils.Core.Models;

public record Plugin(IPlugin PluginInstance, PluginManifest Manifest, JsonNode PluginSettingsSchema);
