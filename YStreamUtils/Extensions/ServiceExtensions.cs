using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json; // 🚀 Required for JsonNamingPolicy
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;
using YStreamUtils.Core.Services.YouTube;
using YStreamUtils.Endpoints;

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
        
        serviceCollection.AddSingleton<YouTubeStreamManager>();
        serviceCollection.AddSingleton<YouTubeChatService>();

        serviceCollection.AddScoped<IDataStore, DbDataStore>();
        serviceCollection.AddScoped<YouTubeStreamService>();
        serviceCollection.AddScoped<YouTubeCredentialService>();
        
        serviceCollection.AddScoped<TenantContext>();

        serviceCollection.AddOpenApi();
        return serviceCollection;
    }
}
