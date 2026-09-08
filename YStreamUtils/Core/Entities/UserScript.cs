using System.ComponentModel.DataAnnotations;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Entities;

public class UserScript
{
    [Key]
    public string ScriptId { get; set; } = string.Empty;
    public EventKey Topic { get; set; }
    public string RawJsString { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}