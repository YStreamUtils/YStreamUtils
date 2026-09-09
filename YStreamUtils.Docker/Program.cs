using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Data;
using YStreamUtils.Extensions;


const string kestrelUrl = "http://localhost:5000";

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(kestrelUrl);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddYStreamUtils();

var app = builder.Build();

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

var isOpenApiGen = Environment.GetEnvironmentVariable("DOTNET_OPENAPI_GENERATION") == "true";

if (!isOpenApiGen)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(); 
        Console.WriteLine("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}
else
{
    Console.WriteLine("[OpenAPI Gen] Skipping database operations during build-time schema extraction.");
}



Console.WriteLine($"[YStreamUtils] Launching web host on {kestrelUrl}");
app.Run();