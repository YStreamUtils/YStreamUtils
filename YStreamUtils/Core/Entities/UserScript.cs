using System.ComponentModel.DataAnnotations;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Entities;

public class UserScript : ITenantEntity
{
    [Key]
    public string ScriptId { get; set; } = string.Empty;
    public EventKey Topic { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string TenantId { get; set; }  = string.Empty;
}