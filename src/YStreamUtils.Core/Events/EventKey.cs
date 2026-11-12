using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Events;

public class EventKey
{
    public const string All = "app:all";
    public const string ManualInvoke = "app:manual";
    public const string StreamChatMessage = "stream:chat_message";
    public const string YoutubeSuperChat = "stream:youtube:superchat";
    public const string ApplicationLog = "app:log";
    public const string StreamMetrics = "stream:metrics";
    public const string StreamDiscovered = "stream:discovered";
    public const string StreamRemoved = "stream:removed";
    
    public static List<string> KnownKeys { get; } = typeof(EventKey)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();
}