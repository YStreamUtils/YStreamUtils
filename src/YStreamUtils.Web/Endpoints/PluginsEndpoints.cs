using Microsoft.AspNetCore.Mvc;
using YStreamUtils.Core.Services;
using YStreamUtils.SDK.Plugin;

namespace YStreamUtils.Web.Endpoints;

public static class PluginsEndpoints
{
    public static RouteGroupBuilder AddPluginService(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("/plugins/registry", async ([FromServices] PluginService pluginService) =>
            {
                var marketplace = await pluginService.FetchAllRegistryPluginsAsync();
                return Results.Ok(marketplace);
            });

        groupBuilder.MapPost("/plugins/install", async (PluginManifest manifest, [FromServices] PluginService pluginService) =>
            {
                await pluginService.DownloadAndInstallPluginAsync(manifest);
                return Results.Ok();
            });

        groupBuilder.MapGet("/plugins/installed", ([FromServices] PluginService pluginService) =>
            {
                var installed = pluginService.GetActivePlugins().Select(s => s.Value.Manifest);
                return Results.Ok(installed);
            });

        groupBuilder.MapGet("/plugins/settings",
            ([FromServices] PluginService pluginService, [FromQuery] string pluginName) => 
            pluginService.GetActivePlugins().TryGetValue(pluginName, out var plugin) 
                ? Results.Json(plugin) 
                : Results.NotFound());

        groupBuilder.MapGet("/plugins/settings/schema",
            ([FromServices] PluginService pluginService, [FromQuery] string pluginName) => 
            pluginService.GetActivePlugins().TryGetValue(pluginName, out var plugin)
                ? Results.Ok((object?)plugin.PluginSettingsSchema.ToJsonString())
                : Results.NotFound());

        return groupBuilder;
    }
}