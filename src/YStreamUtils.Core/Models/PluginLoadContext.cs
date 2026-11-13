using System.Reflection;
using System.Runtime.Loader;

namespace YStreamUtils.Core.Models;

internal sealed class PluginLoadContext(string name, string pluginPath)
    : AssemblyLoadContext($"ALC_{name}", isCollectible: true)
{
    private static readonly HashSet<string> HostAssemblyNames = Default.Assemblies
        .Select(a => a.GetName().Name!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name != null && HostAssemblyNames.Contains(assemblyName.Name))
            return null;

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path == null 
            ? null 
            : LoadFromAssemblyPath(path);
    }
}