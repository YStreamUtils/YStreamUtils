using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Google.Apis.YouTube.v3;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;

namespace YStreamUtils.Core.Models;

public static class Consts
{
    public static string ApplicationDataFolder => ApplicationEnvironment.CurrentPlatform == ApplicationPlatform.Docker
        ? DockerDataFolder
        : LocalDataFolder;

    private static string LocalDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YStreamUtils");

    private static string DockerDataFolder => Environment.GetEnvironmentVariable("DATA_FOLDER") ?? "/data";
    public static readonly IEnumerable<string> YoutubeScopes = [YouTubeService.Scope.YoutubeForceSsl];

    public static readonly JsonSerializerOptions IndentedSerializerOptions = new()
    {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static readonly Lazy<ScriptOptions> DefaultScriptOptions = new(() =>
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
}