using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Photino.NET;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Services;
using YStreamUtils.Extensions;

namespace YStreamUtils.Desktop;

internal static class Program
{
    private const string KestrelUrl = "http://localhost:5000";

    [STAThread]
    private static void Main(string[] args)
    {
        if (EF.IsDesignTime)
        {
            return;
        }
        
        _ = Task.Run(async () =>
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            builder.WebHost.UseUrls(KestrelUrl);
            builder.Services.AddYStreamUtils();

            builder.Services.AddDbContext<SqliteDbContext>();
            builder.Services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<SqliteDbContext>());
            builder.Services.AddScoped<TenantContext>();

            var app = builder.Build();

            app.RunDatabaseMigrations();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            else
            {
                app.UseDefaultFiles();
                app.UseStaticFiles();
            }

            app.Use(async (context, next) =>
            {
                if (context.User?.Identity?.IsAuthenticated != true)
                {
                    var claims = new[] {
                        new Claim(ClaimTypes.NameIdentifier, "local-desktop-user"),
                        new Claim(ClaimTypes.Name, "Desktop"),
                        new Claim(ClaimTypes.Role, "Admin")
                    };
            
                    var identity = new ClaimsIdentity(claims, "DesktopAuth");
                    context.User = new ClaimsPrincipal(identity);
                }
                await next();
            });
            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseDefaultEndpoints();
            
            Console.WriteLine($"[YStreamUtils] Launching web host on {KestrelUrl}");

            await app.RunAsync();
        });

        var app = new PhotinoApplication();
        var mainWindow = new PhotinoWindow();
        mainWindow.SetTitle("YStreamUtils");
        mainWindow.SetChromeless(true);
        mainWindow.SetSize(1024, 800);
        mainWindow.Center();

        mainWindow.RegisterWebMessageReceivedHandler((sender, eventArgs) =>
        {
            if (sender is null) return;
            var window = sender as PhotinoWindow;
            switch (eventArgs.Message)
            {
                case "cmd:begin-drag":
                    window!.BeginWindowDrag();
                    break;
                case "cmd:minimize":
                    window!.Minimize();
                    break;
                case "cmd:maximize":
                    window!.SetMaximized(window.WindowState != PhotinoWindowState.Maximized);
                    break;
                case "cmd:close":
                    window!.Close();
                    break;
            }
        });
        mainWindow.NewWindowRequested += (sender, eventArgs) =>
        {
            OpenInSystemBrowser(eventArgs.Uri.ToString());
        };

#if DEBUG
        mainWindow.Load(new Uri("http://localhost:5173"));
#else
        mainWindow.Load(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html")));
#endif

        app.Run(mainWindow);
    }

    private static void OpenInSystemBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open browser: {ex.Message}");
        }
    }
}

