using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Services;
using YStreamUtils.Endpoints;

var argsList = args.ToList();

var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
                    || argsList.Contains("--dev");

const string kestrelUrl = "http://localhost:5000";

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseUrls(kestrelUrl);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddSingleton<PluginService>();
builder.Services.AddSingleton<ScriptsService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddScoped<IDataStore, DbDataStore>();
builder.Services.AddScoped<YouTubeStreamService>();
builder.Services.AddScoped<YouTubeCredentialService>();

if (isDevelopment)
{
    builder.Environment.EnvironmentName = Environments.Development;
}

builder.Services.AddOpenApi();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}


var apiGroup = app.MapGroup("/api");
apiGroup.AddSettingsService();
apiGroup.AddPluginService();
apiGroup.AddScriptsService();
apiGroup.AddEventBusEndpoints();
apiGroup.AddAuthEndpoints();

apiGroup.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));
apiGroup.MapGet("/version", () => Results.Ok(new { Version = "1.0.0-alpha" }));

Console.WriteLine($"[YStreamUtils] Launching web host on {kestrelUrl}");
app.Run();