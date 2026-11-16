using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using YScriptEngine.Lsp.Roslyn;
using YScriptEngine.Roslyn;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;
using YStreamUtils.Web.Components;
using YStreamUtils.Web.Extensions;

var builder = WebApplication.CreateBuilder();

builder.AddYStreamUtils();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => { options.DetailedErrors = true; });

builder.Services.AddSingleton<RoslynLspProvider>();
builder.Services.AddSingleton<RoslynScriptCompiler>();

builder.Services.AddSingleton<AudioPlaybackService>();
builder.Services.AddHostedService<AudioPlaybackService>(sp => sp.GetRequiredService<AudioPlaybackService>());

builder.Services.AddSingleton<UserLibraryService>();

builder.Services.AddTransient<ScriptOptions>(sp =>
{
    var options = Consts.DefaultScriptOptions.Value;

    var pluginService = sp.GetRequiredService<PluginService>();
    var activePlugins = pluginService.GetActivePlugins();
    if (activePlugins.Count > 0)
    {
        var pluginAssemblies = activePlugins.Values
            .Select(p => p.PluginType.Assembly)
            .Distinct()
            .ToArray();

        var allPluginContextAssemblies = pluginAssemblies
            .Select(AssemblyLoadContext.GetLoadContext)
            .Where(alc => alc != null && alc != AssemblyLoadContext.Default)
            .Distinct()
            .SelectMany(alc => alc!.Assemblies)
            .Concat(pluginAssemblies)
            .Where(ass => !ass.IsDynamic && !string.IsNullOrEmpty(ass.Location))
            .DistinctBy(ass => ass.Location)
            .ToArray();

        var pluginReferences = allPluginContextAssemblies
            .Select(ass => MetadataReference.CreateFromFile(ass.Location));

        var pluginImports = allPluginContextAssemblies
            .SelectMany(ass => ass.ExportedTypes)
            .Select(t => t.Namespace)
            .Where(ns => !string.IsNullOrEmpty(ns))
            .Distinct();

        options = options
            .AddReferences(pluginReferences)
            .AddImports(pluginImports!);
    }

    var userLibrary = sp.GetRequiredService<UserLibraryService>();
    if (userLibrary.CurrentReference is not null)
    {
        options = options
            .AddReferences(userLibrary.CurrentReference)
            .AddImports("UserLibrary");
    }

    return options;
});

var app = builder.Build();

app.RunDatabaseMigrations();
app.UseDefaultEndpoints();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.UseStaticFiles();

var eventBus = app.Services.GetRequiredService<IEventBus>();

var pluginService = app.Services.GetRequiredService<PluginService>();
await pluginService.ReloadLocalPluginsAsync();

var userLibrary = app.Services.GetRequiredService<UserLibraryService>();
var scriptService = app.Services.GetRequiredService<ScriptsService>();
var dbContextFactory = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();

async Task RegisterLibrary()
{
    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
    var library = await dbContext.UserScripts.AsNoTracking().FirstOrDefaultAsync(s => s.ScriptId == "UserLibrary");
    if (library is not null)
    {
        await userLibrary.CompileAndSave(library.Source);
    }
}

async Task RegisterScripts()
{
    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
    var scripts = await dbContext.UserScripts.AsNoTracking().Where(s => s.ScriptId != "UserLibrary").ToListAsync();
    var tasks = scripts.Select(s => scriptService.RegisterScriptAndBindToBusAsync(s.Topic, s.ScriptId, s.Source));
    await Task.WhenAll(tasks);
}

await RegisterLibrary();
await RegisterScripts();

eventBus.Subscribe<EmptyStruct>(EventKey.LibraryChanged, async (_, _) => await RegisterScripts());
eventBus.Subscribe<EmptyStruct>(EventKey.PluginsChanged, async (_, _) =>
{
    await RegisterLibrary();
    await RegisterScripts();
});

app.MapFallbackToFile("index.html");

await app.RunAsync();