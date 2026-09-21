using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Events;

[JsonConverter(typeof(JsonStringEnumConverter<StreamEventName>))]
public enum StreamEventName
{
    [EnumMember(Value = "chat")] Chat,
    [EnumMember(Value = "superchat")] SuperChat,
    [EnumMember(Value = "cheer")] Cheer,
}

public interface IStreamEventData { }

public record StreamEventEnvelope<T>
{ 
    public Platform Platform { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Timestamp { get; init; }
    public T? Data { get; init; }

    public static StreamEventEnvelope<T> Create(Platform platform, T? data = default) => new()
    {
        Platform = platform,
        Timestamp = DateTime.UtcNow,
        Data = data
    };
}

public readonly record struct EmptyStruct : IStreamEventData;

public readonly record struct BaseUserData(
    string AuthorId,
    string Author,
    string AuthorColor
) : IStreamEventData;

public readonly record struct StreamChatMessageEvent(
    BaseUserData User,
    string MessageId,
    string Message,
    string LiveChatId
) : IStreamEventData;

public readonly record struct StreamSuperChatMessageEvent(
    BaseUserData User,
    string MessageId,
    string Message,
    string LiveChatId,
    string Amount
) : IStreamEventData;

public readonly record struct StreamCheerMessageEvent(
    BaseUserData User,
    long Bits
) : IStreamEventData;

public readonly record struct StreamMetricsEvent(string Viewers) : IStreamEventData;