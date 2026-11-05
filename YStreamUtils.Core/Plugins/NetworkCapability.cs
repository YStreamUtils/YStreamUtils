using YStreamUtils.SDK.Plugin;

namespace YStreamUtils.Core.Plugins;

public class NetworkCapability : INetworkCapability
{
    public HttpClient Client { get; }
    public string PluginName { get; }

    public NetworkCapability(HttpClient baseClient, string pluginName)
    {
        PluginName = pluginName;
        Client = baseClient;

        Client.DefaultRequestHeaders.UserAgent.Clear();
        Client.DefaultRequestHeaders.UserAgent.ParseAdd($"YStreamUtils.Core-Plugin/{pluginName}");
    }
}