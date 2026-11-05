using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services.YouTube;

namespace YStreamUtils.Core.Services.Stream;
public class YouTubeStreamService(YouTubeCredentialService credentialService, ILogger<YouTubeStreamService> logger) : IStreamFinderService
{
    public async Task<List<StreamInfo>> GetActiveBroadcastAsync(CancellationToken token = default)
    {
        var streamsReturn = new List<StreamInfo>();
        try
        {
            var client = await credentialService.GetClient(false);
            var streamsRequest = client.LiveBroadcasts.List((string[])["id", "status", "snippet"]);
            streamsRequest.BroadcastType = LiveBroadcastsResource.ListRequest.BroadcastTypeEnum.All;
            streamsRequest.Mine = true;

            var streams = await streamsRequest.ExecuteAsync(token);
            if (streams?.Items == null) return streamsReturn;

            foreach (var stream in streams.Items)
            {
                if (stream?.Status == null || stream.Snippet == null) continue;

                var isLive = stream.Status.LifeCycleStatus == "live" && !string.IsNullOrEmpty(stream.Snippet.LiveChatId);
                if (!isLive) continue;
                
                streamsReturn.Add(new StreamInfo(stream.Id, Platform.YouTube));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to fetch active broadcast info.");
        }

        return streamsReturn;
    }
}
