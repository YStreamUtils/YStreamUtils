using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.Extensions;

namespace YStreamUtils.Endpoints;

public static class EventBusEndpoints
{
    public static RouteGroupBuilder AddEventBusEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost("/events/invoke", async ([FromServices] IEventBus eventBus, [FromServices] IHttpContextAccessor httpContextAccessor) =>
            {
                var tenantId = httpContextAccessor.GetTenantContext().TenantId;
                var envelope = StreamEventEnvelope<object>.Create(
                    tenantId,
                    Platform.YouTube
                );

                await eventBus.PublishAsync(EventKey.ManualInvoke, envelope);

                return Results.Ok(new { Status = "Manual trigger successfully dispatched." });
            })
            .WithName("InvokeManualEvent");

        groupBuilder.MapGet("/events/listen", async (
                HttpContext context,
                IEventBus eventBus,
                IHttpContextAccessor httpContextAccessor, 
                CancellationToken cancellationToken) =>
            {
                var currentTenantId = httpContextAccessor.GetTenantContext().TenantId;

                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.Append("Cache-Control", "no-cache");
                context.Response.Headers.Append("Connection", "keep-alive");

                var channel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
                {
                    SingleReader = true
                });

                var allEventKeys = Enum.GetValues<EventKey>();
                var unsub = allEventKeys.Select(key => eventBus.Subscribe(key, async (payload, cbToken) =>
                    {
                        if (payload is not ITenantEntity tenantEvent || tenantEvent.TenantId != currentTenantId)
                        {
                            return;
                        }

                        await channel.Writer.WriteAsync(payload, cbToken);
                    }))
                    .ToList();

                try
                {
                    while (await channel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        while (channel.Reader.TryRead(out var payload))
                        {
                            await context.Response.WriteAsync("data: ", cancellationToken);
                            
                            await context.Response.WriteAsJsonAsync(payload, payload.GetType(), cancellationToken: cancellationToken);
                            
                            await context.Response.WriteAsync("\n\n", cancellationToken);
                            await context.Response.Body.FlushAsync(cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    channel.Writer.Complete();

                    foreach (var unsubAction in unsub) unsubAction();
                }
            })
            .WithName("ListenToGlobalEventBusStream");

        return groupBuilder;
    }
}
