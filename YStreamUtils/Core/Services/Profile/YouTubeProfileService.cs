using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Services.YouTube;
using YStreamUtils.Endpoints;
using YStreamUtils.Extensions;

namespace YStreamUtils.Core.Services.Profile;

public class YouTubeProfileService(YouTubeCredentialService credentialService, IHttpContextAccessor httpContextAccessor, ILogger<YouTubeProfileService> logger) : IProfileService
{
    public async Task<UserProfile?> GetUserProfile(bool isBot)
    {
        try
        {
            var tenantId = httpContextAccessor.GetTenantContext().TenantId;
            var client = await credentialService.GetClient(tenantId, isBot);
            if (client == null) return null;

            var profileRequest = client.Channels.List((string[])["snippet", "id"]);
            profileRequest.Mine = true;

            var result = await profileRequest.ExecuteAsync();

            var profile = result?.Items.FirstOrDefault();
            if (profile == null) return null;

            return new UserProfile(profile.Snippet.Title, profile.Snippet.Thumbnails.Default__.Url,
                profile.Id, profile.Snippet.CustomUrl);
        }
        catch (InvalidOperationException e)
        {
            logger.LogError(e.Message);
            return null;
        }
    }
}