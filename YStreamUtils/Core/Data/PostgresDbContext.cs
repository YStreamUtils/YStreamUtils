using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace YStreamUtils.Core.Data;

public class PostgresDbContext(
    DbContextOptions<PostgresDbContext> options,
    IConfiguration configuration,
    IDataProtectionProvider? provider = null,
    ILogger<AppDbContext>? logger = null)
    : AppDbContext(options, configuration, provider, logger)
{
    private readonly IConfiguration _configuration = configuration;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        optionsBuilder.UseNpgsql(
            connectionString ?? "Host=localhost;Database=migrations_dummy;Username=postgres;Password=your_secure_password",
            x => x.MigrationsAssembly("YStreamUtils")
        );

        base.OnConfiguring(optionsBuilder);
    }
}