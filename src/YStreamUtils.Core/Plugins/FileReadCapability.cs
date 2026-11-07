using System.Text;
using YStreamUtils.SDK.Plugin;

namespace YStreamUtils.Core.Plugins;

public class FileReadCapability(string pluginDirectory) : IFileReadCapability
{
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