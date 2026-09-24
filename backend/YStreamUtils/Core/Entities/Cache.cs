using System.ComponentModel.DataAnnotations;

namespace YStreamUtils.Core.Entities;

public class Cache
{
  [Key]
  public required string Key {get; init;}
  public required string Value {get; set;} 
}