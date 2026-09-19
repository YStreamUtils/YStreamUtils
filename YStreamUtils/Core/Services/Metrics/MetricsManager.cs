using System.Collections.Concurrent;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services.Metrics;

public class MetricsManager
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeStreams = new();

    public bool IsMetricsStreamRunning(string tenantId, Platform platform, string videoId) =>
        _activeStreams.ContainsKey($"{tenantId}:{platform}-{videoId}");

    public void RegisterMetricsStream(string tenantId, Platform platform, string videoId, CancellationTokenSource cts)
    {
        _activeStreams.TryAdd($"{tenantId}:{platform}-{videoId}", cts);
    }

    public void UnregisterMetricsStream(string tenantId, Platform platform, string videoId)
    {
        if (!_activeStreams.TryRemove($"{tenantId}:{platform}-{videoId}", out var cts)) return;

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