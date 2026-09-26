using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Entities;

public class OAuthToken
{
    [Key] public string TokenId { get; set; } = Guid.NewGuid().ToString("N");

    [Required] public Platform Platform { get; set; } = Platform.YouTube;

    [Required] public bool IsBotAccount { get; set; }

    [Required] public string TokenJson { get; set; } = string.Empty;
}