using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.FileProviders;
using YScriptEngine.Lsp.Roslyn;
using YScriptEngine.Roslyn;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Services;
using YStreamUtils.Web.Components;
using YStreamUtils.Web.Extensions;

var builder = WebApplication.CreateBuilder();

builder.AddYStreamUtils();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => { options.DetailedErrors = true; });

builder.Services.AddSingleton<RoslynLspProvider>();
builder.Services.AddSingleton<RoslynCompiler>();

var baseScriptOptions = new Lazy<ScriptOptions>(() =>
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
        MetadataReference.CreateFromFile(typeof(ServiceProvider).Assembly.Location),
        MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
        MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
        MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Threading.Tasks.dll")),
        MetadataReference.CreateFromFile(Assembly.GetEntryAssembly()!.Location)
    };

    var appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
        .Select(MetadataReference (a) => MetadataReference.CreateFromFile(a.Location));

    var baseReferences = coreAssemblies
        .Concat(appDomainAssemblies)
        .GroupBy(r => r is PortableExecutableReference pe ? pe.FilePath : r.ToString())
        .Select(g => g.First());

    string[] globalImports =
    [
        "System", "System.IO", "System.Text", "System.Text.Json",
        "System.Text.RegularExpressions", "System.Linq",
        "System.Collections.Generic", "System.Threading.Tasks",
        "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.Logging"
    ];

    return ScriptOptions.Default
        .WithReferences(baseReferences)
        .WithImports(globalImports);
});

builder.Services.AddTransient<ScriptOptions>(sp =>
{
    var pluginService = sp.GetRequiredService<PluginService>();
    var activePlugins = pluginService.GetActivePlugins();
    if (activePlugins.Count == 0) return baseScriptOptions.Value;

    var pluginAssemblies = activePlugins.Values
        .Select(p => p.PluginType.Assembly)
        .Distinct()
        .ToArray();

    var pluginReferences = pluginAssemblies
        .Where(ass => !ass.IsDynamic && !string.IsNullOrEmpty(ass.Location))
        .Select(ass => MetadataReference.CreateFromFile(ass.Location));

    var pluginImports = pluginAssemblies
        .SelectMany(ass => ass.ExportedTypes)
        .Select(t => t.Namespace)
        .Where(ns => !string.IsNullOrEmpty(ns))
        .Distinct();

    return baseScriptOptions.Value
        .AddReferences(pluginReferences)
        .AddImports(pluginImports!);
});

var app = builder.Build();

app.RunDatabaseMigrations();
app.UseDefaultEndpoints();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.UseStaticFiles();

var eventBus = app.Services.GetRequiredService<IEventBus>();
Task.Run(async () =>
{
    var tracker = 0;
    while (true)
    {
        await Task.Delay(2000);
        await eventBus.PublishAsync(EventKey.ApplicationLog, $"Started: {tracker++}");
    }
});


app.MapFallbackToFile("index.html");

await app.RunAsync();