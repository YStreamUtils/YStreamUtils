using Microsoft.Extensions.Logging;

namespace YStreamUtils.Core.Services;

using Google.Apis.YouTube.v3;

public class YouTubeStreamService(YouTubeCredentialService credentialService, ILogger<YouTubeStreamService> logger)
{
    public async Task<List<string>> PollTenantStreamsAsync(string tenantId)
    {
        logger.LogInformation("Polling YouTube streams using main user credentials for tenant {TenantId}.", tenantId);
        
        var client = await credentialService.GetClient(tenantId, false);
        if  (client == null) return [];
        
        var streamsRequest = client.LiveBroadcasts.List((string[])["id", "status", "snippet"]);
        streamsRequest.BroadcastType = LiveBroadcastsResource.ListRequest.BroadcastTypeEnum.All;
        streamsRequest.Mine = true;
        
        var streams = await streamsRequest.ExecuteAsync();
        if (streams.Items == null) return [];
            
        List<string> liveIDs = [];
        foreach (var stream in streams.Items)
        {
            if (stream.Status == null || stream.Snippet == null)
            {
                continue;
            }
            var status = stream.Status.LifeCycleStatus;
                
            var isLive = status == "live" && !string.IsNullOrEmpty(stream.Snippet.LiveChatId);

            if (isLive)
            {
                liveIDs.Add(stream.Id);
            }
                
        }
        return liveIDs;

    }

    
}
