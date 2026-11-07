using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services.YouTube;

public class YouTubeCredentialService(
    IDbContextFactory<AppDbContext> contextFactory,
    IDataStore dataStore,
    ILogger<YouTubeCredentialService> logger)
{
    private async Task<UserCredential> GetCredential(bool isBot)
    {
        var dbContext = await contextFactory.CreateDbContextAsync();
        var settings = await dbContext.OAuthConfigs.FirstOrDefaultAsync(x => x.Platform == Platform.YouTube);
        if (settings == null || string.IsNullOrEmpty(settings.ClientId) || string.IsNullOrEmpty(settings.ClientSecret))
        {
            throw new InvalidOperationException("Please configure your Google Client ID and Secret in settings first.");
        }

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = settings.ClientId, ClientSecret = settings.ClientSecret },
            Scopes = Consts.YoutubeScopes,
            DataStore = dataStore
        });
        var userType = isBot ? "bot" : "user";
        var token = await flow.LoadTokenAsync(userType, CancellationToken.None);
        return token != null ? new UserCredential(flow, userType, token) : throw new Exception("Invalid token");
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