using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter<Platform>))]
public enum Platform
{
    YouTube,
    Twitch,
    None
}