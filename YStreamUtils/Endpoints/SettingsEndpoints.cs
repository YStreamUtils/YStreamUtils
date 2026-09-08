using Microsoft.AspNetCore.Mvc;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Endpoints;

public static class SettingsServiceEndpointsExtension
{
    public static RouteGroupBuilder AddSettingsService(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("/settings",
                ([FromServices] SettingsService settingsService) => {
                    try
                    {
                        return Results.Ok(settingsService.GetSettings());
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                })
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