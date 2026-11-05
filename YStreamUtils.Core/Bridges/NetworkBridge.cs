using System.Net.Http.Json;

namespace YStreamUtils.Core.Bridges;

public class NetworkBridge(HttpClient client)
{
    public async Task<string> GetStringAsync(string url) 
        => await client.GetStringAsync(url);

    public async Task<string> PostJsonAsync(string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        return await response.Content.ReadAsStringAsync();
    }
}