using Microsoft.EntityFrameworkCore;
using Photino.NET;
using YStreamUtils.Core.Data;
using YStreamUtils.Extensions;

namespace YStreamUtils.Desktop;

internal static class Program
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

#if DEBUG
        mainWindow.Load(new Uri("http://localhost:5173"));
#else
        mainWindow.Load(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html")));
#endif

        app.Run(mainWindow);
    }
}