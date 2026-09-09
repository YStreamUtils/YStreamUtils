using Microsoft.EntityFrameworkCore;
using Photino.NET;
using YStreamUtils.Core.Data;
using YStreamUtils.Extensions;

namespace YStreamUtils.Desktop;

internal class Program
{
    private const string KestrelUrl = "http://localhost:5000";

    [STAThread]
    private static void Main(string[] args)
    {
        _ = Task.Run(async () =>
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            builder.WebHost.UseUrls(KestrelUrl);
            builder.Services.AddYStreamUtils();

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

            app.UseDefaultEndpoints();
            Console.WriteLine($"[YStreamUtils] Launching web host on {KestrelUrl}");

            await app.RunAsync();
        });

        var app = new PhotinoApplication();
        var mainWindow = new PhotinoWindow();
        mainWindow.SetTitle("YStreamUtils");
        mainWindow.SetChromeless(true);

#if DEBUG
        mainWindow.Load(new Uri("http://localhost:5173"));
#else
        mainWindow.Load(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html")));
#endif

        app.Run(mainWindow);
    }
}