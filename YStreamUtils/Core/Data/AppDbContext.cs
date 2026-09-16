using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Add this namespace
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Core.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options, 
    TenantContext tenantContext,
    IConfiguration configuration,
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
        
        var dbProvider = configuration["Database:Provider"]?.ToLowerPercent() ?? "sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (dbProvider == "postgres")
        {
            optionsBuilder.UseNpgsql(connectionString ?? throw new InvalidOperationException("Postgres connection string is missing."));
        }
        else
        {
            var dbPath = Path.Combine(Consts.ApplicationDataFolder, "ystreamutils.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            optionsBuilder.UseSqlite(connectionString ?? $"Data Source={dbPath}");
        }
        
        if (provider != null && logger != null)
        {
            optionsBuilder.AddInterceptors(new DataProtectionInterceptor(logger, provider));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        var filterMethod = typeof(AppDbContext)
            .GetMethod(nameof(ConfigureTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!entityType.ClrType.IsAssignableTo(typeof(ITenantEntity))) continue;
            var genericMethod = filterMethod?.MakeGenericMethod(entityType.ClrType);
            genericMethod?.Invoke(this, [modelBuilder]);
        }
    }

    private void ConfigureTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == tenantContext.TenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantId();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantId();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantId()
    {
        var entries = ChangeTracker.Entries();
        foreach (var entry in entries)
        {
            if (entry is { State: EntityState.Added, Entity: ITenantEntity tenantEntity })
            {
                tenantEntity.TenantId = tenantContext.TenantId;
            }
        }
    }
}

public static class StringExtensions
{
    public static string ToLowerPercent(this string input) => input.ToLowerInvariant().Trim();
}
