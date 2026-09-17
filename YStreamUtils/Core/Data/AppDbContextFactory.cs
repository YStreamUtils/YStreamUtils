using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Core.Data;

public class SqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ystreamutils", "ystreamutils.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        var mockContext = new TenantContext(new HttpContextAccessor(), fallbackTenant: "migrations-design");

        return new SqliteDbContext(optionsBuilder.Options, mockContext, configuration);
    }
}

public class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresDbContext>
{
    public PostgresDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<PostgresDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Uses a hardcoded local fallback password so the design-time tool builds cleanly without a container
        optionsBuilder.UseNpgsql(connectionString ?? "Host=localhost;Database=migrations_dummy;Username=postgres;Password=passwordpassword123");

        var mockContext = new TenantContext(new HttpContextAccessor(), fallbackTenant: "migrations-design");

        return new PostgresDbContext(optionsBuilder.Options, mockContext, configuration);
    }
}
