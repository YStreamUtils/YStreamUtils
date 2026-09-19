using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;
using YStreamUtils.Core.Services.Chat;
using YStreamUtils.Core.Services.Metrics;
using YStreamUtils.Core.Services.Profile;
using YStreamUtils.Core.Services.YouTube;

namespace YStreamUtils.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddYStreamUtils(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient();
        serviceCollection.AddHttpContextAccessor();
        serviceCollection.AddAuthentication();
        serviceCollection.AddAuthorization();
        
        serviceCollection.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
        });

        serviceCollection.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
        });

        serviceCollection.AddSingleton<IEventBus, EventBus>();
        serviceCollection.AddSingleton<PluginService>();
        serviceCollection.AddSingleton<ScriptsService>();
        serviceCollection.AddSingleton<SettingsService>();

        serviceCollection.AddSingleton<MetricsManager>();
        serviceCollection.AddKeyedScoped<IMetricsService, YouTubeMetricsService>(Platform.YouTube);
        
        serviceCollection.AddSingleton<ChatManager>();
        serviceCollection.AddKeyedScoped<IChatService, YouTubeChatService>(Platform.YouTube);
        
        serviceCollection.AddKeyedScoped<IProfileService, YouTubeProfileService>(Platform.YouTube);
        
        serviceCollection.AddScoped<IDataStore, DbDataStore>();
        serviceCollection.AddScoped<YouTubeCredentialService>();
        
        serviceCollection.AddScoped<TenantContext>();

        serviceCollection.AddOpenApi();
        return serviceCollection;
    }
}
