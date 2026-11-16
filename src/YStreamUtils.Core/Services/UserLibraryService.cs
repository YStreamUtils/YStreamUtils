using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Events;

namespace YStreamUtils.Core.Services;

public sealed class UserLibraryService(IServiceProvider serviceProvider, ILogger<UserLibraryService> logger, IDbContextFactory<AppDbContext> factory, IEventBus eventBus)
{
    private const string LibraryAssemblyPrefix = "UserLibrary_";
    private AssemblyLoadContext? _libraryAlc;

    public string SourceCode { get; private set; } = DefaultTemplate;
    public MetadataReference? CurrentReference { get; private set; }
    public Assembly? CurrentAssembly { get; private set; }

    private const string DefaultTemplate = """
                                           namespace UserLibrary;

                                           // Add reusable classes, records, or extension methods here.
                                           // Everything in 'UserLibrary' is automatically available in your scripts!
                                           public static class Helpers
                                           {
                                               public static string Greet(string name) => $"Hello, {name}!";
                                           }
                                           """;

    public async Task<EmitResult?> CompileAndSave(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            SourceCode = string.Empty;
            CurrentReference = null;
            CurrentAssembly = null;
            _libraryAlc?.Unload();
            _libraryAlc = null;
            return null;
        }

        var scriptOptions = serviceProvider.GetRequiredService<ScriptOptions>();
        var references = scriptOptions.MetadataReferences
            .OfType<PortableExecutableReference>()
            .Where(r => r.Display?.StartsWith(LibraryAssemblyPrefix, StringComparison.OrdinalIgnoreCase) != true);

        var assemblyName = $"{LibraryAssemblyPrefix}{Guid.NewGuid():N}";
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions, path: "UserLibrary.cs");
        
        var globalUsingsCode = string.Join("\n", scriptOptions.Imports.Select(ns => $"global using {ns};"));
        logger.LogInformation(globalUsingsCode);
        var globalUsingsTree = CSharpSyntaxTree.ParseText(globalUsingsCode, parseOptions, path: "GlobalUsings.g.cs");

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: [syntaxTree, globalUsingsTree],
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                usings: scriptOptions.Imports));

        using var peStream = new MemoryStream();
        var result = compilation.Emit(peStream);

        if (!result.Success)
            return result;

        SourceCode = code;
        CurrentReference = MetadataReference.CreateFromImage(peStream.ToArray());

        _libraryAlc?.Unload();
        _libraryAlc = new AssemblyLoadContext("UserLibraryAlc", isCollectible: true);
        _libraryAlc.Resolving += (_, name) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));

        peStream.Seek(0, SeekOrigin.Begin);
        CurrentAssembly = _libraryAlc.LoadFromStream(peStream);
        
        var dbContext = await factory.CreateDbContextAsync();
        var exists = await dbContext.UserScripts.FirstOrDefaultAsync(us => us.ScriptId == "UserLibrary");
        if (exists != null)
        {
            exists.Source = SourceCode;
        }
        else
        {
            await dbContext.UserScripts.AddAsync(new UserScript
            {
                ScriptId = "UserLibrary",
                Source = SourceCode
            });
        }
        await dbContext.SaveChangesAsync();
        await eventBus.PublishAsync(EventKey.LibraryChanged, new EmptyStruct());

        return result;
    }
    
    public IDisposable? EnterLibraryContext()
    {
        return _libraryAlc?.EnterContextualReflection();
    }
}
