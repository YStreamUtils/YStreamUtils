using System.ComponentModel.DataAnnotations;

namespace YStreamUtils.Core.Entities;

public class PluginSettings
{
    [Key]
    public required string PluginName { get; set; }
    
    public required string SettingsJson { get; set; }
}