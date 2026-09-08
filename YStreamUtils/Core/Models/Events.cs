using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter<StreamEventName>))]
public enum StreamEventName
{
    [EnumMember(Value = "chat")]
    Chat,
    
    [EnumMember(Value = "superchat")]
    Superchat,
    
    [EnumMember(Value = "cheer")]
    Cheer
}
public record EmptyStruct;

public record StreamEventEnvelope<T>
{
    [JsonPropertyName("event")]
    public StreamEventName Event { get; init; }

    [JsonPropertyName("platform")]
    public Platform Platform { get; init; }

    [JsonPropertyName("timestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Timestamp { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; } = default;

    public static StreamEventEnvelope<T> Create(StreamEventName eventName, Platform platform, T? data = default) => new()
    {
        Event = eventName,
        Platform = platform,
        Timestamp = DateTime.UtcNow,
        Data = data
    };
}

public record BaseUserData(
    [property: JsonPropertyName("authorId")] string AuthorId,
    [property: JsonPropertyName("author")] string Author,
    [property: JsonPropertyName("authorColor")] string AuthorColor,
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("message")] string Message
);

public record StreamChatMessageEvent(
    string AuthorId,
    string Author,
    string AuthorColor,
    string MessageId,
    string Message,
    [property: JsonPropertyName("liveChatId")] string LiveChatId
) : BaseUserData(AuthorId, Author, AuthorColor, MessageId, Message);

public record StreamSuperChatMessageEvent(
    string AuthorId,
    string Author,
    string AuthorColor,
    string MessageId,
    string Message,
    string LiveChatId,
    [property: JsonPropertyName("amount")] string Amount
) : StreamChatMessageEvent(AuthorId, Author, AuthorColor, MessageId, Message, LiveChatId);

public record StreamCheerMessageEvent(
    string AuthorId,
    string Author,
    string AuthorColor,
    string MessageId,
    string Message,
    [property: JsonPropertyName("bits")] long Bits
) : BaseUserData(AuthorId, Author, AuthorColor, MessageId, Message);