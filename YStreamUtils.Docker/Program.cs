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
builder.Services.AddYStreamUtils();

builder.Services.AddScoped<TenantContext>(sp =>
{
    var httpAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new TenantContext(httpAccessor, fallbackTenant: "UNAUTHORIZED_WEB_REQUEST");
});

if (config["Database:Provider"] == "postgres")
{
    builder.Services.AddDbContext<AppDbContext, PostgresDbContext>();
}
else
{
    builder.Services.AddDbContext<AppDbContext, SqliteDbContext>();
}

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

app.UseAuthentication();
app.UseAuthorization();



Console.WriteLine($"[YStreamUtils] Launching web host on {kestrelUrl}");
app.Run();