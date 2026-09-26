using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;
using YStreamUtils.Core.Services.Chat;
using YStreamUtils.Core.Services.Metrics;
using YStreamUtils.Endpoints;

namespace YStreamUtils.Core.Services.YouTube;

public class YouTubeStreamMonitor(
    AppDbContext dbContext,
    YouTubeStreamService streamService,
    YouTubeChatService chatService,
    YouTubeMetricsService metricsService,
    ILogger<YouTubeStreamMonitor> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeStreams = new();
    private DateTime _lastDiscoveryChecks = DateTime.UtcNow;

    public bool IsStreamRunning(string videoId) => 
        _activeStreams.ContainsKey(videoId);

    public void RegisterStream(string videoId, CancellationTokenSource cts)
    {
        _activeStreams.TryAdd(videoId, cts);
    }

    public void UnregisterStream(string videoId)
    {
        if (!_activeStreams.TryRemove(videoId, out var cts)) return;
        try { cts.Cancel(); }
        catch
        {
            // ignored
        }

        cts.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var config = await AuthEndpoints.GetConfigByPlatformQuery(dbContext, Platform.YouTube);

                    if (config is null) throw new Exception("YouTube config not found.");

                    var now = DateTime.UtcNow;

                    if (now - _lastDiscoveryChecks < TimeSpan.FromMinutes(3)) continue;
                    _lastDiscoveryChecks = now;

                    var broadcast = await streamService.GetActiveBroadcastAsync(stoppingToken);

                    if (broadcast.Count == 0)
                    {
                        foreach (var stream in _activeStreams)
                        {
                            UnregisterStream(stream.Key);
                        }

                        return;
                    }

                    foreach (var (targetVideoId, _) in broadcast)
                    {
                        if (IsStreamRunning(targetVideoId)) continue;


                        logger.LogInformation("Discovered active broadcast {VideoId}.", targetVideoId);

                        var streamCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        RegisterStream(targetVideoId, streamCts);

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await metricsService.StartMetricStream(targetVideoId, streamCts.Token);
                                await chatService.StartChatStream(targetVideoId, streamCts.Token);
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                logger.LogError(ex, "Live metrics task crashed for video {VideoId}.", targetVideoId);
                            }
                            finally
                            {
                                UnregisterStream(targetVideoId);
                            }
                        }, stoppingToken);
                    }

            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error occurred during YouTube monitoring loop iteration.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task StopMetricsServiceForVideo(string videoId)
    {
        try
        {
            await metricsService.StopMetricStream(videoId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing StopMetricStream cleanly for video {VideoId}.", videoId);
        }
    }
}
