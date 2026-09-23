using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
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
    public static WebApplicationBuilder AddYStreamUtils(this WebApplicationBuilder builder)
    {
        var serviceCollection = builder.Services;
        serviceCollection.AddHttpClient();
        
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
        serviceCollection.AddKeyedSingleton<IMetricsService, YouTubeMetricsService>(Platform.YouTube);

        serviceCollection.AddSingleton<ChatManager>();
        serviceCollection.AddKeyedSingleton<IChatService, YouTubeChatService>(Platform.YouTube);

        serviceCollection.AddKeyedSingleton<IProfileService, YouTubeProfileService>(Platform.YouTube);

        serviceCollection.AddSingleton<IDataStore, DbDataStore>();
        serviceCollection.AddSingleton<YouTubeCredentialService>();
        
        builder.Services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type != typeof(EventKey)) return Task.CompletedTask;
                
                schema.Type = JsonSchemaType.String;
                schema.Properties?.Clear();

                schema.Enum = EventKey.KnownKeys.Select(JsonNode (x) => JsonValue.Create(x.Value)).ToList();
                return Task.CompletedTask;
            });
        });
        return builder;
    }
}
