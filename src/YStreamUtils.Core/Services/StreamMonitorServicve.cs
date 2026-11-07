using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services.Chat;
using YStreamUtils.Core.Services.Metrics;

namespace YStreamUtils.Core.Services;

public record struct StreamInfo(string StreamId, Platform Platform);
public class StreamMonitorService(IServiceProvider serviceProvider, ILogger<StreamMonitorService> logger, IEventBus eventBus)
{
    
    private readonly ConcurrentDictionary<StreamInfo, CancellationTokenSource> _activeSessions = new();
    
    public void UpdateActiveStreams(List<StreamInfo> currentLiveStreams)
    {
        var incomingStreamIds = currentLiveStreams.ToHashSet();

        foreach (var activeId in _activeSessions.Keys)
        {
            if (!incomingStreamIds.Contains(activeId))
            {
                StopMonitoring(activeId);
            }
        }

        foreach (var stream in currentLiveStreams.Where(stream => !_activeSessions.ContainsKey(stream)))
        {
            StartMonitoring(stream);
        }
    }

    private void StartMonitoring(StreamInfo streamInfo)
    {
        var cts = new CancellationTokenSource();
        if (!_activeSessions.TryAdd(streamInfo, cts)) return;

        logger.LogInformation("Starting chat and metrics monitoring for {Platform} stream: {StreamId}", streamInfo.Platform, streamInfo.StreamId);

        var chatService = serviceProvider.GetRequiredKeyedService<IChatService>(streamInfo.Platform);
        

        Task.Run(() => chatService.StartChatStream(streamInfo.StreamId, cts.Token), cts.Token);
        Task.Run(() => StartMetricsLoopAsync(streamInfo.StreamId, streamInfo.Platform, cts.Token), cts.Token);

        var data = StreamEventEnvelope<StreamDiscoveredEvent>.Create(streamInfo.Platform, new StreamDiscoveredEvent(streamInfo));
        eventBus.PublishAsync(EventKey.StreamDiscovered, data, cts.Token);
    }
    
    private void StopMonitoring(StreamInfo streamInfo)
    {
        if (!_activeSessions.TryRemove(streamInfo, out var cts)) return;
        
        logger.LogInformation("Stream {StreamId} went offline. Stopping monitoring tasks.", streamInfo.StreamId);
        cts.Cancel();
        cts.Dispose();
        
        var data = StreamEventEnvelope<StreamRemovedEvent>.Create(streamInfo.Platform, new StreamRemovedEvent(streamInfo));
        eventBus.PublishAsync(EventKey.StreamDiscovered, data, cts.Token);
    }

    private async Task StartMetricsLoopAsync(string streamId, Platform platform, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        var metricsService = serviceProvider.GetRequiredKeyedService<IMetricsService>(platform);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    var metrics = await metricsService.GetMetrics(streamId, ct);
                    var payload = StreamEventEnvelope<StreamMetricsEvent>.Create(platform, new StreamMetricsEvent(streamId, metrics.ViewerCount));
                    await eventBus.PublishAsync(EventKey.StreamMetrics, payload, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching metrics for stream {StreamId}", streamId);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    
}
