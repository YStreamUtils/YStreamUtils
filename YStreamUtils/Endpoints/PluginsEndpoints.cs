using Microsoft.AspNetCore.Mvc;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;

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
            .Produces<PluginManifest>()
            .WithName("GetInstalledPlugins");

        return groupBuilder;
    }
}