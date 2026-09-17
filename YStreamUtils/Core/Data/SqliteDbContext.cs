using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Core.Data;

public class SqliteDbContext(
    DbContextOptions<SqliteDbContext> options,
    TenantContext tenantContext,
    IConfiguration configuration,
    IDataProtectionProvider? provider = null,
    ILogger<AppDbContext>? logger = null)
    : AppDbContext(options, tenantContext, configuration, provider, logger)
{
    private readonly IConfiguration _configuration = configuration;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var dbPath = Path.Combine(Consts.ApplicationDataFolder, "ystreamutils.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        optionsBuilder.UseSqlite(
            connectionString ?? $"Data Source={dbPath}",
            x => x.MigrationsAssembly("YStreamUtils")
        );

        base.OnConfiguring(optionsBuilder);
    }
}