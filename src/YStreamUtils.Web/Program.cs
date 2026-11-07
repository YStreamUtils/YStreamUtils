using Microsoft.Extensions.FileProviders;
using YStreamUtils.Web.Components;
using YStreamUtils.Web.Extensions;

var builder = WebApplication.CreateBuilder();

builder.AddYStreamUtils();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.RunDatabaseMigrations();
app.UseDefaultEndpoints();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
}
else
{
    var manifestProvider = new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot");
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = manifestProvider
    });
}

app.MapFallbackToFile("index.html");

await app.RunAsync();