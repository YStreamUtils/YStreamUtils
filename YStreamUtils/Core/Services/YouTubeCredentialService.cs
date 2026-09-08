using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Models;
using YStreamUtils.Endpoints;

namespace YStreamUtils.Core.Services;

public class YouTubeCredentialService(AppDbContext db, IDataStore dataStore, ILogger<YouTubeCredentialService> logger)
{
    private async Task<UserCredential?> GetCredential(string tenantId, bool isBot)
    {
        var settings = await AuthEndpoints.GetConfigByPlatformQuery(db, Platform.YouTube);

        if (settings == null || string.IsNullOrEmpty(settings.ClientId) || string.IsNullOrEmpty(settings.ClientSecret))
        {
            logger.LogWarning("Skipping tenant {TenantId}: Missing Client configuration.", tenantId);
            return null;
        }

        
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = settings.ClientId, ClientSecret = settings.ClientSecret },
            Scopes = Consts.YoutubeScopes,
            DataStore = dataStore
        });
        var userType = isBot ? "bot" : "user";
        var storageKey = $"{tenantId}-{userType}";
        var token = await flow.LoadTokenAsync(storageKey, CancellationToken.None);
        return token == null ? null : new UserCredential(flow, storageKey, token);
    }

    public async Task<YouTubeService?> GetClient(string tenantId, bool isBot)
    {
        var credential = await GetCredential(tenantId, isBot);
        if (credential == null) return null;
        return new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
        });
        
    }
}