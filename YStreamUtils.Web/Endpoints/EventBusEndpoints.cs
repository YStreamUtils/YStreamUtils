using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Web.Endpoints;

public static class EventBusEndpoints
{
    public static RouteGroupBuilder AddEventBusEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost("/events/invoke", async ([FromServices] IEventBus eventBus) =>
            {
                var envelope = StreamEventEnvelope<EmptyStruct>.Create(
                    Platform.YouTube,
                    new EmptyStruct()
                );

                await eventBus.PublishAsync(EventKey.ManualInvoke, envelope);

                return Results.Ok(new { Status = "Manual trigger successfully dispatched." });
            })
            .WithName("InvokeManualEvent");

        groupBuilder.MapGet("/events/listen", async (
                [FromQuery] EventKey[]? eventKey,
                HttpContext context,
                [FromServices] IEventBus eventBus,
                CancellationToken cancellationToken) =>
            {
                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.Append("Cache-Control", "no-cache");
                context.Response.Headers.Append("Connection", "keep-alive");

                var channel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
                {
                    SingleReader = true
                });

                var events = eventKey ?? [EventKey.All]; 
                var unsub = events.Select(key => eventBus.Subscribe(key, async (payload, cbToken) =>
                    {
                        await channel.Writer.WriteAsync(payload, cbToken);
                    }))
                    .ToList();
                try
                {
                    while (await channel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        while (channel.Reader.TryRead(out var payload))
                        {
                            var runtimeType = payload.GetType();
                            var eventKeyToken = Events.KnownTypeRegistry
                                .FirstOrDefault(x => x.Value == runtimeType).Key;

                            var eventName = eventKeyToken.Value ?? "app:unknown";
                            
                            await context.Response.WriteAsync($"event: {eventName}\n", cancellationToken);
                            
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
            .WithName("ListenToGlobalEventBusStream")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");


        return groupBuilder;
    }
}
