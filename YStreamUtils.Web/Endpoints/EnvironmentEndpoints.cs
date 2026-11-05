using YStreamUtils.Core.Models;

namespace YStreamUtils.Web.Endpoints;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record EnvironmentPlatformResponse(ApplicationPlatform Platform);
public static class EnvironmentEndpoints
{
    public static RouteGroupBuilder AddEnvironmentEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/environment/platform", () =>
        {
            var platformInfo = new EnvironmentPlatformResponse(ApplicationEnvironment.CurrentPlatform);

            return Results.Ok(platformInfo);
        })
        .Produces<EnvironmentPlatformResponse>()
        .WithName("GetRunningEnvironment");
        
        return builder;
    }
}