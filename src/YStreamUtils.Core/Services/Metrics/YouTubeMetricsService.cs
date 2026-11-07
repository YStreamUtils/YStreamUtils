using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services.YouTube;

namespace YStreamUtils.Core.Services.Metrics;

public class YouTubeMetricsService(
    ILogger<YouTubeMetricsService> logger,
    YouTubeCredentialService credentialService) : IMetricsService
{
    private const Platform CurrentPlatform = Platform.YouTube;

    public async Task<StreamMetrics> GetMetrics(string videoId, CancellationToken token = default)
    {
        try
        {
            var client = await credentialService.GetClient(false);
            var request = client.Videos.List("liveStreamingDetails");
            request?.Id = videoId;

            var response = await request!.ExecuteAsync(token);
            var videoItem = response.Items?.FirstOrDefault();

            if (videoItem?.LiveStreamingDetails == null) throw new InvalidOperationException();

            return new StreamMetrics(
                videoItem.LiveStreamingDetails.ConcurrentViewers.ToString() ?? throw new InvalidOperationException());
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to fetch metrics for YouTube video {VideoId}.",
                videoId);
            throw;
        }
    }
}