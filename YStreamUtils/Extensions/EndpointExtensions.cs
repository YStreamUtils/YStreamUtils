

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using YStreamUtils.Endpoints;

namespace YStreamUtils.Extensions;

public static class EndpointExtensions
{
    public static WebApplication UseDefaultEndpoints(this WebApplication app)
    {
        
        var apiGroup = app.MapGroup("/api");
        apiGroup.AddSettingsService();
        apiGroup.AddPluginService();
        apiGroup.AddScriptsService();
        apiGroup.AddEventBusEndpoints();
        apiGroup.AddAuthEndpoints();
        apiGroup.AddEnvironmentEndpoints();

        apiGroup.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));
        apiGroup.MapGet("/version", () => Results.Ok(new { Assembly.GetExecutingAssembly().GetName().Version }));
        return app;
    }
}