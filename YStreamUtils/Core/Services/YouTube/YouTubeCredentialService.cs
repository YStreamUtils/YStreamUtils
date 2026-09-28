using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Models;
using YStreamUtils.Endpoints;

namespace YStreamUtils.Core.Services.YouTube;

public class YouTubeCredentialService(IDbContextFactory<AppDbContext> contextFactory, IDataStore dataStore, ILogger<YouTubeCredentialService> logger)
{
    private async Task<UserCredential> GetCredential(bool isBot)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var settings = await AuthEndpoints.GetConfigByPlatformQuery(dbContext, Platform.YouTube);

        if (settings == null || string.IsNullOrEmpty(settings.ClientId) || string.IsNullOrEmpty(settings.ClientSecret))
        {
            throw new Exception("Missing Client configuration.");
        }

        
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = settings.ClientId, ClientSecret = settings.ClientSecret },
            Scopes = Consts.YoutubeScopes,
            DataStore = dataStore
        });
        var userType = isBot ? "bot" : "user";
        var storageKey = $"{userType}";
        var token = await flow.LoadTokenAsync(storageKey, CancellationToken.None);
        return token != null ? new UserCredential(flow, storageKey, token) : throw new Exception("Invalid token");
    }

    public async Task<YouTubeService> GetClient(bool isBot)
    {
        var credential = await GetCredential(isBot);
        return new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
        });
        
    }
}