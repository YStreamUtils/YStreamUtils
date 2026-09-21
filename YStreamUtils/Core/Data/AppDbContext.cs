using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Entities;

namespace YStreamUtils.Core.Data;

public abstract class AppDbContext(
    DbContextOptions options, 
    IConfiguration configuration,
    IDataProtectionProvider? provider = null,
    ILogger<AppDbContext>? logger = null) 
    : DbContext(options)
{
    public DbSet<UserScript> UserScripts => Set<UserScript>();
    public DbSet<OAuthConfig> OAuthConfigs => Set<OAuthConfig>();
    public DbSet<OAuthToken> OAuthTokens => Set<OAuthToken>();
    public DbSet<Cache> Caches => Set<Cache>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        if (provider != null && logger != null)
        {
            optionsBuilder.AddInterceptors(new DataProtectionInterceptor(logger, provider));
        }
    }
}
