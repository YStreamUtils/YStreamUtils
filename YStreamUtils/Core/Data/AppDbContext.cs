using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Core.Data;

public abstract class AppDbContext(
    DbContextOptions options, 
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
        base.OnConfiguring(optionsBuilder);
        
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
