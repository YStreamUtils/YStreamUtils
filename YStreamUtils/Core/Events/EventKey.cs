using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Events;

[JsonConverter(typeof(JsonStringEnumConverter<EventKey>))] 
public enum EventKey
{
    [JsonStringEnumMemberName("app:manual")]
    ManualInvoke,

    [JsonStringEnumMemberName("stream:chat_message")]
    StreamChatMessage,

    [JsonStringEnumMemberName("stream:youtube:superchat")]
    YoutubeSuperChat,

    [JsonStringEnumMemberName("app:log")]
    ApplicationLog
}