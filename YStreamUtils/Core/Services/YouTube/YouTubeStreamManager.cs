using System.Collections.Concurrent;

namespace YStreamUtils.Core.Services.YouTube;

public class YouTubeStreamManager
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeStreams = new();

    public bool IsStreamRunning(string tenantId, string videoId) => 
        _activeStreams.ContainsKey($"{tenantId}-{videoId}");

    public void RegisterStream(string tenantId, string videoId, CancellationTokenSource cts)
    {
        _activeStreams.TryAdd($"{tenantId}-{videoId}", cts);
    }

    public void UnregisterStream(string tenantId, string videoId)
    {
        if (!_activeStreams.TryRemove($"{tenantId}-{videoId}", out var cts)) return;
        
        try { cts.Cancel(); } catch { /* Already canceled */ }
        cts.Dispose();
    }
}