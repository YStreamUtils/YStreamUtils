using System.Text;

namespace YStreamUtils.Core.Plugins;

public class FileReadCapability(string pluginDirectory, string pluginName) : IFileReadCapability
{
    public string PluginName { get; } = pluginName;

    public async Task<string> ReadTextAsync(string relativePath)
    {
        var cleanFileName = Path.GetFileName(relativePath);
        var secureFullPath = Path.Combine(pluginDirectory, cleanFileName);

        if (!File.Exists(secureFullPath))
        {
            throw new FileNotFoundException($"The file target '{cleanFileName}' was not found inside the plugin workspace directory.");
        }

        return await File.ReadAllTextAsync(secureFullPath, Encoding.UTF8);
    }
}