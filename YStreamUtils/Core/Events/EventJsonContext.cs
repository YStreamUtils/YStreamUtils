using System.Text.Json.Serialization;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Events;

// Enums
[JsonSerializable(typeof(StreamEventName))]
[JsonSerializable(typeof(EventKey))]

// Data Types
[JsonSerializable(typeof(EmptyStruct))]
[JsonSerializable(typeof(BaseUserData))]
[JsonSerializable(typeof(StreamChatMessageEvent))]
[JsonSerializable(typeof(StreamSuperChatMessageEvent))]
[JsonSerializable(typeof(StreamCheerMessageEvent))]

// Event Envelopes
[JsonSerializable(typeof(StreamEventEnvelope<EmptyStruct>))]
[JsonSerializable(typeof(StreamEventEnvelope<StreamChatMessageEvent>))]
[JsonSerializable(typeof(StreamEventEnvelope<StreamSuperChatMessageEvent>))]
[JsonSerializable(typeof(StreamEventEnvelope<StreamCheerMessageEvent>))]
public partial class EventJsonContext : JsonSerializerContext
{
}