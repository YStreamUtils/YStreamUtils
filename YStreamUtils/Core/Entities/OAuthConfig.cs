using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Entities;

public class OAuthConfig : ITenantEntity
{
    [Key]
    public Platform Platform { get; set; } = Platform.YouTube;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}