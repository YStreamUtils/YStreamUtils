using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services.YouTube;
using YStreamUtils.Extensions;

namespace YStreamUtils.Core.Services.Metrics;

public class YouTubeMetricsService(
    ILogger<YouTubeMetricsService> logger,
    IHttpContextAccessor contextAccessor,
    MetricsManager metricsManager,
    YouTubeCredentialService credentialService,
    IEventBus eventBus) : IMetricsService
{
    private const Platform CurrentPlatform = Platform.YouTube;

    public async Task StartMetricStream(string videoId, CancellationToken token = default)
    {
        var tenantId = contextAccessor.GetTenantContext().TenantId;

        if (metricsManager.IsMetricsStreamRunning(tenantId, CurrentPlatform, videoId))
        {
            logger.LogWarning("Metric stream for YouTube video {VideoId} under Tenant {TenantId} is already running.",
                videoId, tenantId);
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        metricsManager.RegisterMetricsStream(tenantId, CurrentPlatform, videoId, cts);

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            logger.LogInformation("Started YouTube metric background loop for video {VideoId} (Tenant: {TenantId})",
                videoId, tenantId);

            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    try
                    {
                        var client = await credentialService.GetClient(tenantId, false);
                        var request = client.Videos.List("liveStreamingDetails");
                        request?.Id = videoId;

                        var response = await request!.ExecuteAsync(cts.Token)!;
                        var videoItem = response.Items?.FirstOrDefault();

                        if (videoItem?.LiveStreamingDetails == null) continue;
                        
                        var data = new StreamMetricsEvent(
                            videoItem.LiveStreamingDetails.ConcurrentViewers.ToString() ?? "undefined");
                        var envelope =
                            StreamEventEnvelope<StreamMetricsEvent>.Create(tenantId, CurrentPlatform, data);
                        await eventBus.PublishAsync(EventKey.StreamMetrics, envelope, cancellationToken: cts.Token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Failed to resolve scoped dependencies or fetch metrics for YouTube video {VideoId}.",
                            videoId);
                        throw;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("YouTube metric loop cleanly stopped for video {VideoId}.", videoId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal crash in metric background thread for video {VideoId}.", videoId);
            }
            finally
            {
                metricsManager.UnregisterMetricsStream(tenantId, CurrentPlatform, videoId);
            }
        }, token);

        await Task.CompletedTask;
    }

    public Task StopMetricStream(string videoId)
    {
        var tenantId = contextAccessor.GetTenantContext().TenantId;
        metricsManager.UnregisterMetricsStream(tenantId, CurrentPlatform, videoId);

        logger.LogInformation("Stop request sent to MetricsManager for YouTube video {VideoId}.", videoId);
        return Task.CompletedTask;
    }
}