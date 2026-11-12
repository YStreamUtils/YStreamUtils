using System.Text.Json.Nodes;
using YStreamUtils.SDK.Plugin;

namespace YStreamUtils.Core.Models;

public record Plugin(PluginManifest Manifest, JsonNode PluginSettingsSchema);
