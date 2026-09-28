using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Services;
using YStreamUtils.Extensions;


const string kestrelUrl = "http://localhost:5000";

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.WebHost.UseUrls(kestrelUrl);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.AddYStreamUtils();

if (config["Database:Provider"] == "postgres")
{
    builder.Services.AddDbContextFactory<PostgresDbContext>();
    
    builder.Services.AddSingleton<IDbContextFactory<AppDbContext>>(sp => 
        new DbContextFactoryBridge<PostgresDbContext>(sp.GetRequiredService<IDbContextFactory<PostgresDbContext>>()));
}
else
{
    builder.Services.AddDbContextFactory<SqliteDbContext>();
    
    builder.Services.AddSingleton<IDbContextFactory<AppDbContext>>(sp => 
        new DbContextFactoryBridge<SqliteDbContext>(sp.GetRequiredService<IDbContextFactory<SqliteDbContext>>()));
}
builder.Services.AddTransient<AppDbContext>(sp => 
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

var app = builder.Build();

app.RunDatabaseMigrations();
app.UseDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}



Console.WriteLine($"[YStreamUtils] Launching web host on {kestrelUrl}");
app.Run();