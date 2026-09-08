using System.Text.Json.Serialization;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Endpoints;

public record OAuthConfigInput(string TenantId, Platform Platform, string ClientId, string ClientSecret);
public record ConnectUrlResponse(string Url);
public record UserProfile(
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("avatarUrl")] string AvatarUrl,
    [property: JsonPropertyName("channelId")] string ChannelId,
    [property: JsonPropertyName("handle")] string Handle
);


public static class AuthEndpoints
{
    public static readonly Func<AppDbContext, Platform, Task<OAuthConfig?>> GetConfigByPlatformQuery =
        EF.CompileAsyncQuery((AppDbContext db, Platform platform) =>
            db.OAuthConfigs.FirstOrDefault(x => x.Platform == platform));
    public static RouteGroupBuilder AddAuthEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/auth/config", async ([FromBody] OAuthConfigInput input, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(input.ClientId) || string.IsNullOrWhiteSpace(input.ClientSecret))
            {
                return Results.BadRequest("Client ID and Client Secret cannot be empty.");
            }

            var config = await db.OAuthConfigs.FirstOrDefaultAsync(x => x.Platform == input.Platform);
            if (config == null)
            {
                config = new OAuthConfig
                {
                    Platform = input.Platform,
                };
                db.OAuthConfigs.Add(config);
            }

            config.ClientId = input.ClientId;
            config.ClientSecret = input.ClientSecret;

            await db.OAuthTokens.Where(x => x.Platform == input.Platform).ExecuteDeleteAsync();

            await db.SaveChangesAsync();
            return Results.Ok($"{Enum.GetName(input.Platform)} OAuth configuration saved successfully.");
        })
        .Produces<string>()
        .WithName("SaveAuthConfig");

        builder.MapGet("/auth/config", async ([FromQuery] Platform platform, [FromServices] AppDbContext db) =>
        {
            return await db.OAuthConfigs.AsNoTracking().Where(x => x.Platform == platform).AnyAsync();
        })
        .Produces<bool>()
        .WithName("HasAuthConfig");
        
        builder.MapGet("/auth/login/youtube", async (
            [FromQuery] string tenantId,
            [FromQuery] string role,
            [FromServices] AppDbContext db, 
            [FromServices] IDataStore dataStore, 
            HttpContext context) =>
        {
            var settings = await GetConfigByPlatformQuery(db, Platform.YouTube);
            if (settings == null || string.IsNullOrEmpty(settings.ClientId) || string.IsNullOrEmpty(settings.ClientSecret))
            {
                return Results.BadRequest("Please configure your Google Client ID and Secret in settings first.");
            }

            var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/api/auth/callback/youtube";

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = settings.ClientId,
                    ClientSecret = settings.ClientSecret
                },
                Scopes = Consts.YoutubeScopes, 
                DataStore = dataStore
            });

            var authorizationUrl = (GoogleAuthorizationCodeRequestUrl)flow.CreateAuthorizationCodeRequest(redirectUri);
            authorizationUrl.AccessType = "offline";
            authorizationUrl.Prompt = "consent";
            
            authorizationUrl.State = $"{tenantId}:{role}";
            
            var responseData = new ConnectUrlResponse(authorizationUrl.Build().ToString());

            return Results.Json(responseData);
        })
        .Produces<ConnectUrlResponse>()
        .WithName("YoutubeLoginUrl");

        builder.MapGet("/auth/callback/youtube", async (
            [FromQuery] string code, 
            [FromQuery] string state,
            [FromServices] AppDbContext db, 
            [FromServices] IDataStore dataStore, 
            HttpContext context) =>
        {
            var settings = await GetConfigByPlatformQuery(db, Platform.YouTube);
            if (settings == null || string.IsNullOrEmpty(settings.ClientId) || string.IsNullOrEmpty(settings.ClientSecret))
            {
                return Results.BadRequest("Please configure your Google Client ID and Secret in settings first.");
            }
            
            var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/api/auth/callback/youtube";

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = settings.ClientId,
                    ClientSecret = settings.ClientSecret
                },
                Scopes = Consts.YoutubeScopes,
                DataStore = dataStore
            });

            var stateParts = state.Split(':');
            if (stateParts.Length < 2)
            {
                return Results.BadRequest("Invalid authorization state returned from server context.");
            }
            
            var tenantId = stateParts[0];
            var role = stateParts[1];
            var localizedStorageKey = $"{tenantId}-{role}";

            var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                userId: localizedStorageKey, 
                code: code, 
                redirectUri: redirectUri, 
                CancellationToken.None
            );

            return Results.Ok($"YouTube successfully linked as a {role}! You can close this tab and return to the application.");
        });

        builder.MapGet("/auth/profile", async ([FromServices] YouTubeCredentialService credentialService, [FromQuery] string tenantId, [FromQuery] Platform platform, [FromQuery] bool isBot) =>
        {
            var client = await credentialService.GetClient(tenantId, isBot);
            if (client == null) return Results.NotFound();
            
            var profileRequest = client.Channels.List((string[])["snippet", "id"]);
            profileRequest.Mine = true;
            
            var result = await profileRequest.ExecuteAsync();
            if (result == null) return Results.NotFound();
            
            var profile = result.Items.FirstOrDefault();
            if(profile == null) return Results.NotFound();
            
            var userProfile = new UserProfile(profile.Snippet.Title, profile.Snippet.Thumbnails.Default__.Url,
                profile.Id, profile.Snippet.CustomUrl);
            
            return Results.Json(userProfile);
        })
        .Produces<UserProfile>()
        .WithName("GetProfile");
        
        return builder;
    }
}
