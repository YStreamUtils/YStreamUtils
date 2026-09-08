using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Endpoints;

public static class EventBusEndpoints
{
    public static RouteGroupBuilder AddEventBusEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost("/events/invoke", async ([FromServices] IEventBus eventBus) =>
            {
                var envelope = StreamEventEnvelope<object>.Create(
                    StreamEventName.Chat,
                    Platform.YouTube
                );

                await eventBus.PublishAsync(EventKey.ManualInvoke, envelope);

                return Results.Ok(new { Status = "Manual trigger successfully dispatched." });
            })
            .WithName("InvokeManualEvent");

        groupBuilder.MapGet("/events/listen", async (
                HttpContext context,
                IEventBus eventBus,
                CancellationToken cancellationToken) =>
            {
                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.Append("Cache-Control", "no-cache");
                context.Response.Headers.Append("Connection", "keep-alive");

                var responseStream = context.Response.BodyWriter;

                var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
                {
                    SingleReader = true
                });

                var allEventKeys = Enum.GetValues<EventKey>();
                var unsub = allEventKeys.Select(key => eventBus.Subscribe(key, async (payload, cbToken) =>
                    {
                        var json = JsonSerializer.Serialize(payload, payload.GetType());

                        await channel.Writer.WriteAsync($"data: {json}\n\n", cbToken);
                    }))
                    .ToList();

                try
                {
                    while (await channel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        while (channel.Reader.TryRead(out var message))
                        {
                            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                            await responseStream.WriteAsync(bytes, cancellationToken);
                            await responseStream.FlushAsync(cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    channel.Writer.Complete();

                    foreach (var unsubAction in unsub)
                    {
                        unsubAction();
                    }
                }
            })
            .WithName("ListenToGlobalEventBusStream");

        return groupBuilder;
    }
}