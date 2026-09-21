using System.Collections.Concurrent;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services.Metrics;

public class MetricsManager
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeStreams = new();

    public bool IsMetricsStreamRunning(Platform platform, string videoId) =>
        _activeStreams.ContainsKey($"{platform}-{videoId}");

    public void RegisterMetricsStream(Platform platform, string videoId, CancellationTokenSource cts)
    {
        _activeStreams.TryAdd($"{platform}-{videoId}", cts);
    }

    public void UnregisterMetricsStream(Platform platform, string videoId)
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