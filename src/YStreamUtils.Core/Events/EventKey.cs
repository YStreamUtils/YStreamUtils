using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Events;

public class EventKey
{
    // APPLICATION EVENTS
    public const string All = "app:all";
    public const string ManualInvoke = "app:manual";
    public const string PluginsChanged = "app:plugins:changed";
    public const string LibraryChanged = "app:library:changed";
    
    // STREAM EVENTS
    public const string StreamChatMessage = "stream:chat_message";
    public const string YoutubeSuperChat = "stream:youtube:superchat";
    public const string ApplicationLog = "app:log";
    public const string StreamMetrics = "stream:metrics";
    public const string StreamDiscovered = "stream:discovered";
    public const string StreamRemoved = "stream:removed";
    
    //AUDIO EVENTS
    public const string AudioPlay = "audio:play";
    public const string AudioSkip = "audio:skip";
    public const string AudioClear = "audio:clear";
    public const string AudioEnded = "audio:ended";
    
    public static List<string> KnownKeys { get; } = typeof(EventKey)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();
}