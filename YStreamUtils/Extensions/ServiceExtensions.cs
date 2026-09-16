using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;
using YStreamUtils.Endpoints;

namespace YStreamUtils.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddYStreamUtils(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient();
        serviceCollection.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Add(EventJsonContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Add(ModelJsonContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Add(EntityJsonContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Add(AuthEndpointJsonContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Add(EnvironmentEndpointJsonContext.Default);
        });

        serviceCollection.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.PropertyNamingPolicy = null;
            options.SerializerOptions.WriteIndented = true;
        });

        serviceCollection.AddSingleton<IEventBus, EventBus>();
        serviceCollection.AddSingleton<PluginService>();
        serviceCollection.AddSingleton<ScriptsService>();
        serviceCollection.AddSingleton<SettingsService>();
        serviceCollection.AddDbContext<AppDbContext>();
        serviceCollection.AddScoped<IDataStore, DbDataStore>();
        serviceCollection.AddScoped<YouTubeStreamService>();
        serviceCollection.AddScoped<YouTubeCredentialService>();

        serviceCollection.AddOpenApi();
        return serviceCollection;
    }
}