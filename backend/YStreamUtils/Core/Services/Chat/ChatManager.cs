using System.Collections.Concurrent;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services.Chat;

public class ChatManager
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeStreams = new();

    public bool IsChatStreamRunning(Platform platform, string videoId) =>
        _activeStreams.ContainsKey($"{platform}-{videoId}");

    public void RegisterChatStream(Platform platform, string videoId, CancellationTokenSource cts)
    {
        _activeStreams.TryAdd($"{platform}-{videoId}", cts);
    }

    public void UnregisterChatStream(Platform platform, string videoId)
    {
        if (!_activeStreams.TryRemove($"{platform}-{videoId}", out var cts)) return;

        try
        {
            cts.Cancel();
        }
        catch
        {
            /* Already canceled */
        }

        cts.Dispose();
    }
}