using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Entities;

[JsonSerializable(typeof(OAuthToken))]
[JsonSerializable(typeof(List<OAuthToken>))]
[JsonSerializable(typeof(OAuthConfig))]
[JsonSerializable(typeof(List<OAuthConfig>))]
[JsonSerializable(typeof(UserScript))]
[JsonSerializable(typeof(List<UserScript>))]
public partial class EntityJsonContext : JsonSerializerContext { } 