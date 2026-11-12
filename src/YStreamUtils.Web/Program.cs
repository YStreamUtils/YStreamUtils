using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.FileProviders;
using YScriptEngine.Lsp.Roslyn;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.Web.Components;
using YStreamUtils.Web.Extensions;

var builder = WebApplication.CreateBuilder();

builder.AddYStreamUtils();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });

builder.Services.AddSingleton<RoslynLspProvider>();
builder.Services.AddSingleton<ScriptOptions>(sp =>
        {
            var coreAssemblyLocation = typeof(object).Assembly.Location;
            var runtimeDirectory = Path.GetDirectoryName(coreAssemblyLocation)!;

            var coreAssemblies = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(coreAssemblyLocation),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
                MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Threading.Tasks.dll")),
                MetadataReference.CreateFromFile(Assembly.GetEntryAssembly()!.Location) 
            };

            var appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(MetadataReference (a) => MetadataReference.CreateFromFile(a.Location));

            var allReferences = coreAssemblies
                .Concat(appDomainAssemblies)
                .GroupBy(r => r is PortableExecutableReference pe ? pe.FilePath : r.ToString()) 
                .Select(g => g.First())
                .ToArray();

            string[] globalImports =
            [
                "System", 
                "System.IO", 
                "System.Text", 
                "System.Text.Json", 
                "System.Text.RegularExpressions", 
                "System.Linq",
                "System.Collections.Generic", 
                "System.Threading.Tasks"
            ];

            return ScriptOptions.Default
                .WithReferences(Enumerable.Empty<Assembly>()) 
                .AddReferences(allReferences)                 
                .WithImports(globalImports);
        });

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

var eventBus = app.Services.GetRequiredService<IEventBus>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
Task.Run(async () =>
{
    var tracker = 0;
    while (true)
    {
        await Task.Delay(2000);
        await eventBus.PublishAsync(EventKey.ApplicationLog, $"Started: {tracker++}");
    }
});

var provider = app.Services.GetRequiredService<RoslynLspProvider>();
foreach (var (_, value) in Events.KnownTypeRegistry)
{
    var scriptContextGeneric = typeof(ScriptContext<>).MakeGenericType(value);
    provider.PreCacheBaseSolution(scriptContextGeneric);
}

app.MapFallbackToFile("index.html");

await app.RunAsync();