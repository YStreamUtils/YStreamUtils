using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter<Platform>))]
public enum Platform
{
    [EnumMember(Value = "youtube")]
    YouTube,
    
    [EnumMember(Value = "twitch")]
    Twitch
}