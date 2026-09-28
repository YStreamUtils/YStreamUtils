using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YStreamUtils.Core.Services;
using YStreamUtils.SDK.Plugin;

namespace YStreamUtils.Endpoints;

public static class PluginsEndpoints
{
    public static RouteGroupBuilder AddPluginService(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("/plugins/registry", async ([FromServices] PluginService pluginService) =>
            {
                var marketplace = await pluginService.FetchAllRegistryPluginsAsync();
                return Results.Ok(marketplace);
            })
            .Produces<List<PluginManifest>>()
            .WithName("FetchAllRegistryPlugins");

        groupBuilder.MapPost("/plugins/install", async (PluginManifest manifest, [FromServices] PluginService pluginService) =>
            {
                await pluginService.DownloadAndInstallPluginAsync(manifest);
                return Results.Ok();
            })
            .WithName("DownloadAndInstallPlugin");

        groupBuilder.MapGet("/plugins/installed", ([FromServices] PluginService pluginService) =>
            {
                var installed = pluginService.GetActivePlugins().Select(s => s.Value.Manifest);
                return Results.Ok(installed);
            })
            .Produces<List<PluginManifest>>()
            .WithName("GetInstalledPlugins");

        groupBuilder.MapGet("/plugins/settings",
            ([FromServices] PluginService pluginService, [FromQuery] string pluginName) =>
            {
                return pluginService.GetActivePlugins().TryGetValue(pluginName, out var plugin) 
                    ? Results.Json(plugin) 
                    : Results.NotFound();
            })
            .WithName("GetPluginSettings");

        groupBuilder.MapGet("/plugins/settings/schema",
            ([FromServices] PluginService pluginService, [FromQuery] string pluginName) =>
            {
                return pluginService.GetActivePlugins().TryGetValue(pluginName, out var plugin)
                    ? Results.Ok(plugin.PluginSettingsSchema.ToJsonString())
                    : Results.NotFound();
            })
            .Produces<string>()
            .WithName("GetPluginSettingsSchema");

        return groupBuilder;
    }
}