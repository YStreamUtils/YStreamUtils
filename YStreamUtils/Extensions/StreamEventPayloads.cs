using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using YStreamUtils.Core.Events;

namespace YStreamUtils.Extensions;

public static class OpenApiEventExtensions
{
    public static RouteHandlerBuilder WithStreamEventPayloads(this RouteHandlerBuilder builder)
    {
        return builder
            .Produces<StreamEventEnvelope<StreamChatMessageEvent>>(contentType: "text/event-stream")
            .Produces<StreamEventEnvelope<StreamSuperChatMessageEvent>>(contentType: "text/event-stream")
            .Produces<StreamEventEnvelope<StreamCheerMessageEvent>>(contentType: "text/event-stream")
            .Produces<StreamEventEnvelope<StreamMetricsEvent>>(contentType: "text/event-stream");
    }
}