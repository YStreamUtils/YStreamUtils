using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Events;

[JsonConverter(typeof(EventKeyJsonConverter))] 
public readonly record struct EventKey : IParsable<EventKey>
{
    public string Value { get; }

    private EventKey(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }

    public static EventKey ManualInvoke => new("app:manual");
    public static EventKey StreamChatMessage => new("stream:chat_message");
    public static EventKey YoutubeSuperChat => new("stream:youtube:superchat");
    public static EventKey ApplicationLog => new("app:log");
    public static EventKey StreamMetrics => new("stream:metrics");

    public static EventKey Custom(string customValue) => new(customValue);
    public override string ToString() => Value;

    public static List<EventKey> KnownKeys => typeof(EventKey)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(p => p.PropertyType == typeof(EventKey) && p.Name != nameof(KnownKeys))
        .Select(p => (EventKey)p.GetValue(null)!)
        .ToList();

    public static bool TryParse(string? s, IFormatProvider? provider, out EventKey result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = default;
            return false;
        }

        result = Custom(s);
        return true;
    }

    public static EventKey Parse(string s, IFormatProvider? provider)
    {
        return TryParse(s, provider, out var result) 
            ? result 
            : throw new FormatException($"String '{s}' cannot be parsed into an EventKey.");
    }
}

public class EventKeyJsonConverter : JsonConverter<EventKey>
{
    public override EventKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return EventKey.Custom(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, EventKey value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}