using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services.Auth;
using YStreamUtils.Core.Services.Profile;

namespace YStreamUtils.Web.Endpoints;

public record OAuthConfigInput(Platform Platform, string ClientId, string ClientSecret);

public record ConnectUrlResponse(string Url);



public static class AuthEndpoints
{
    public static RouteGroupBuilder AddAuthEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/auth/config", async ([FromBody] OAuthConfigInput input, [FromServices] IDbContextFactory<AppDbContext> db, HttpContext context) =>
            {
                if (string.IsNullOrWhiteSpace(input.ClientId) || string.IsNullOrWhiteSpace(input.ClientSecret))
                {
                    return Results.BadRequest("Client ID and Client Secret cannot be empty.");
                }

                await using var dbContext = await db.CreateDbContextAsync(context.RequestAborted);

                var config = await dbContext.OAuthConfigs.FirstOrDefaultAsync(x => x.Platform == input.Platform);
                if (config == null)
                {
                    config = new OAuthConfig
                    {
                        Platform = input.Platform,
                    };
                    dbContext.OAuthConfigs.Add(config);
                }

                config.ClientId = input.ClientId;
                config.ClientSecret = input.ClientSecret;

                await dbContext.OAuthTokens.Where(x => x.Platform == input.Platform).ExecuteDeleteAsync();

                await dbContext.SaveChangesAsync();
                return Results.Ok($"{Enum.GetName(input.Platform)} OAuth configuration saved successfully.");
            })
            .Produces<string>()
            .WithName("SaveAuthConfig");

        builder.MapGet("/auth/config", async ([FromQuery] Platform platform, [FromServices] IDbContextFactory<AppDbContext> db, HttpContext context) =>
            {
                var dbContext = await db.CreateDbContextAsync(context.RequestAborted);
                return await dbContext.OAuthConfigs.AsNoTracking().Where(x => x.Platform == platform).AnyAsync();
            })
            .Produces<bool>()
            .WithName("HasAuthConfig");
        
        builder.MapGet("/auth/connect", async (
                [FromQuery] Platform platform,
                [FromQuery] bool bot,
                [FromServices] IDbContextFactory<AppDbContext> db,
                [FromServices] IServiceProvider serviceProvider,
                HttpContext context) =>
            {
                await using var dbContext = await db.CreateDbContextAsync(context.RequestAborted);
                var settings = await dbContext.OAuthConfigs.FirstOrDefaultAsync(x => x.Platform == platform);
    
                if (settings == null || string.IsNullOrEmpty(settings.ClientId))
                {
                    return Results.BadRequest($"Please configure {platform} client credentials first.");
                }

                var authService = serviceProvider.GetRequiredKeyedService<IAuthService>(platform);

                try
                {
                    await authService.AuthenticateAsync(settings, bot, context.RequestAborted);
                }
                catch (Exception e)
                {
                    return Results.BadRequest(e.Message);
                }
                

                return Results.Ok();
            })
            .WithName("ConnectPlatform");

        builder.MapGet("/auth/profile",
                async ([FromServices] IServiceProvider serviceProvider, [FromQuery] Platform platform,
                    [FromQuery] bool isBot) =>
                {
                    var profileService = serviceProvider.GetRequiredKeyedService<IProfileService>(platform);
                    var profile = await profileService.GetUserProfile(isBot);

                    return profile is not null
                        ? Results.Json(profile)
                        : Results.NotFound();
                })
            .Produces<UserProfile>()
            .WithName("GetProfile");
        return builder;
    }
}