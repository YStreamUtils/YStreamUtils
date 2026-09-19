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

public readonly record struct EmptyStruct;

public record StreamEventEnvelope<T> : ITenantEntity
{
    public required string TenantId { get; set; }
    public Platform Platform { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Timestamp { get; init; }
    public T? Data { get; init; }

    public static StreamEventEnvelope<T> Create(string tenantId, Platform platform, T? data = default) => new()
    {
        TenantId = tenantId,
        Platform = platform,
        Timestamp = DateTime.UtcNow,
        Data = data
    };
}

public readonly record struct BaseUserData(
    string AuthorId,
    string Author,
    string AuthorColor
);

public readonly record struct StreamChatMessageEvent(
    BaseUserData User,
    string MessageId,
    string Message,
    string LiveChatId
);

public readonly record struct StreamSuperChatMessageEvent(
    BaseUserData User,
    string MessageId,
    string Message,
    string LiveChatId,
    string Amount
);

public readonly record struct StreamCheerMessageEvent(
    BaseUserData User,
    long Bits
);

public readonly record struct StreamMetricsEvent(string Viewers);