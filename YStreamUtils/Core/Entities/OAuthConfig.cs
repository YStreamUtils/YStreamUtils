using System.ComponentModel.DataAnnotations;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Entities;

public class OAuthConfig
{
    [Key]
    public Platform Platform { get; set; } = Platform.YouTube;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}