using System.Text;
using YStreamUtils.SDK.Plugin;

namespace YStreamUtils.Core.Plugins;

public class FileWriteCapability(string pluginDirectory, string pluginName) : IFileWriteCapability
{
    public string PluginName { get; } = pluginName;

    public async Task WriteTextAsync(string relativePath, string content)
    {
        var cleanFileName = Path.GetFileName(relativePath);
        var secureFullPath = Path.Combine(pluginDirectory, cleanFileName);

        Directory.CreateDirectory(pluginDirectory);

        await File.WriteAllTextAsync(secureFullPath, content, Encoding.UTF8);
    }
}