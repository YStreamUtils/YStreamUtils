using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Entities;

[PrimaryKey(nameof(Platform), nameof(IsBot))]
public class OAuthTokens
{
    [Required]
    [MaxLength(50)]
    public Platform Platform { get; set; } = Platform.YouTube;

    public bool IsBot { get; set; }

    [Required]
    public string TokenJson { get; set; } = string.Empty;
}