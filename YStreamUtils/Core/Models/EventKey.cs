using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter<EventKey>))] 
public enum EventKey
{
    [EnumMember(Value = "app:manual")]
    ManualInvoke,

    [EnumMember(Value = "stream:chat_message")]
    StreamChatMessage,

    [EnumMember(Value = "stream:youtube:superchat")]
    YoutubeSuperChat,

    [EnumMember(Value = "app:log")]
    ApplicationLog
}

public static class EventKeyExtensions
{
    private static readonly Dictionary<EventKey, string> DisplayNames = new()
    {
        { EventKey.ManualInvoke, "Manual Invoke" },
        { EventKey.StreamChatMessage, "Stream Chat Message (all)" },
        { EventKey.YoutubeSuperChat, "YouTube SuperChat" },
        { EventKey.ApplicationLog, "Application Logs" }
    };

    public static string ToDisplayName(this EventKey key) => 
        DisplayNames.TryGetValue(key, out var name) ? name : key.ToString();
}