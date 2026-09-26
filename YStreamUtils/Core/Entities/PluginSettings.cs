using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Entities;

public class PluginSettings
{
    [Key]
    public required string PluginName { get; set; }
    
    public required string SettingsJson { get; set; }
}