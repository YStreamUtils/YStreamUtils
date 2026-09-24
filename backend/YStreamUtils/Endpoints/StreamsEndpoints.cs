using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services.Chat;
using YStreamUtils.Core.Services.Metrics;

namespace YStreamUtils.Endpoints;

public static class StreamsEndpoints
{
    public static IEndpointRouteBuilder AddStreamsServices(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/stream/chat/connect",
                async ([FromQuery] Platform platform, [FromQuery] string videoId,
                    [FromServices] IServiceProvider serviceProvider) =>
                {
                    try
                    {
                        var chatService = serviceProvider.GetRequiredKeyedService<IChatService>(platform);
                        await chatService.StartChatStream(videoId);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }

                    return Results.Ok();
                })
            .WithName("RegisterChatStream");

        routeBuilder.MapDelete("/stream/chat/disconnect",
                ([FromQuery] Platform platform, [FromQuery] string videoId,
                    [FromServices] IServiceProvider serviceProvider) =>
                {
                    try
                    {
                        try
                        {
                            var chatService = serviceProvider.GetRequiredKeyedService<IChatService>(platform);
                            chatService.StopChatStream(videoId);
                        }
                        catch (Exception e)
                        {
                            return Task.FromResult(Results.BadRequest(e.Message));
                        }

                        return Task.FromResult(Results.Ok());
                    }
                    catch (Exception exception)
                    {
                        return Task.FromException<IResult>(exception);
                    }
                })
            .WithName("RemoveChatStream");

        routeBuilder.MapGet("/stream/metrics/connect",
                async ([FromQuery] Platform platform, [FromQuery] string videoId,
                    [FromServices] IServiceProvider serviceProvider) =>
                {
                    try
                    {
                        var metricsService = serviceProvider.GetRequiredKeyedService<IMetricsService>(platform);
                        await metricsService.StartMetricStream(videoId);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }

                    return Results.Ok();
                })
            .WithName("RegisterMetricsStream");

        routeBuilder.MapDelete("/stream/metrics/disconnect",
                async ([FromQuery] Platform platform, [FromQuery] string videoId,
                    [FromServices] IServiceProvider serviceProvider) =>
                {
                    try
                    {
                        var metricsService = serviceProvider.GetRequiredKeyedService<IMetricsService>(platform);
                        await metricsService.StopMetricStream(videoId);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }

                    return Results.Ok();
                })
            .WithName("RemoveMetricsStream");

        return routeBuilder;
    }
}