using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options, 
    IDataProtectionProvider? provider = null,
    ILogger<AppDbContext>? logger = null) 
    : DbContext(options)
{
    public DbSet<UserScript> UserScripts => Set<UserScript>();
    public DbSet<OAuthConfig> OAuthConfigs => Set<OAuthConfig>();
    public DbSet<OAuthToken> OAuthTokens => Set<OAuthToken>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;
        
        var dbPath = Path.Combine(Consts.ApplicationDataFolder, "ystreamutils.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        
        if (provider != null && logger != null)
        {
            optionsBuilder.AddInterceptors(new DataProtectionInterceptor(logger, provider));
        }
    }
}
