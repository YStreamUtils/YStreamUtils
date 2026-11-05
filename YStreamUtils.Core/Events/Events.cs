using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Core.Events;

public static class Events
{
    public static readonly Dictionary<EventKey, Type> KnownTypeRegistry = new()
    {
        { EventKey.All, typeof(object) },
        { EventKey.StreamChatMessage, typeof(StreamEventEnvelope<StreamChatMessageEvent>) },
        { EventKey.YoutubeSuperChat, typeof(StreamEventEnvelope<StreamSuperChatMessageEvent>) },
        { EventKey.ManualInvoke, typeof(StreamEventEnvelope<EmptyStruct>) },
        { EventKey.StreamDiscovered, typeof(StreamEventEnvelope<StreamDiscoveredEvent>) },
        { EventKey.StreamRemoved, typeof(StreamEventEnvelope<StreamRemovedEvent>) },
        { EventKey.StreamMetrics, typeof(StreamEventEnvelope<StreamMetricsEvent>) },
    };
}

[UsedImplicitly]
public enum StreamEventName
{
    [EnumMember(Value = "chat")] Chat,
    [EnumMember(Value = "superchat")] SuperChat,
    [EnumMember(Value = "cheer")] Cheer,
}

public interface IStreamEventData
{
}

public record StreamEventEnvelope<T>
{
    [UsedImplicitly]     
    public required Platform Platform { get; init; }

    [UsedImplicitly] 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required DateTime? Timestamp { get; init; }

    [UsedImplicitly]
    public required T Data { get; init; }

    public static StreamEventEnvelope<T> Create(Platform platform, T data) => new()
    {
        Platform = platform,
        Timestamp = DateTime.UtcNow,
        Data = data
    };
}

public readonly record struct EmptyStruct : IStreamEventData;

[UsedImplicitly]
public readonly record struct StreamDiscoveredEvent(StreamInfo StreamInfo) : IStreamEventData;

[UsedImplicitly]
public readonly record struct StreamRemovedEvent(StreamInfo StreamInfo) : IStreamEventData;

[UsedImplicitly]
public readonly record struct BaseUserData(
    string AuthorId,
    string Author,
    string AuthorColor
) : IStreamEventData;

[UsedImplicitly]
public readonly record struct StreamChatMessageEvent(
    BaseUserData User,
    string MessageId,
    string Message,
    string LiveChatId
) : IStreamEventData;

[UsedImplicitly]
public readonly record struct StreamSuperChatMessageEvent(
    BaseUserData User,
    string MessageId,
    string Message,
    string LiveChatId,
    string Amount
) : IStreamEventData;

[UsedImplicitly]
public readonly record struct StreamCheerMessageEvent(
    BaseUserData User,
    long Bits
) : IStreamEventData;

[UsedImplicitly]
public readonly record struct StreamMetricsEvent(string VideoId, string Viewers) : IStreamEventData;