using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services.Auth;

public class YouTubeAuthService(IDataStore dataStore) : IAuthService
{
    public async Task AuthenticateAsync(OAuthConfig config, bool isBot, CancellationToken ct = default)
    {
        var secrets = new ClientSecrets 
        { 
            ClientId = config.ClientId, 
            ClientSecret = config.ClientSecret 
        };
        
        var role = isBot ? "bot" : "user";
        
        UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Consts.YoutubeScopes,
            user: role,
            taskCancellationToken: ct,
            dataStore: dataStore
        );
    }
}