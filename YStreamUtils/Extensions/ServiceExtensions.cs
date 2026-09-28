using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Logging;
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
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.FormatterName = "HumanReadable";
        });
        
        builder.Logging.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
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
            
            options.AddDocumentTransformer(async (document, context, cancellationToken) =>
            {
                var envelopeSchema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Required = new HashSet<string> { "event", "data" },
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["event"] = new OpenApiSchema { Type = JsonSchemaType.String },
                        ["data"] = new OpenApiSchema
                        {
                            OneOf = new List<IOpenApiSchema>()
                        }
                    }
                };

                foreach (var (eventKey, closedType) in Events.KnownTypeRegistry)
                {
                    var payloadType = closedType.GetGenericArguments()[0];
                    var payloadSchema = await context.GetOrCreateSchemaAsync(payloadType, cancellationToken: cancellationToken);
            
                    var componentName = payloadType.Name.Replace("`", ""); 
                    document.Components.Schemas.TryAdd(componentName, payloadSchema);

                    envelopeSchema.Properties["data"].OneOf.Add(new OpenApiSchema
                    {
                        DynamicRef = componentName
                    });
                }

                document.Components.Schemas.TryAdd("StreamEventEnvelope", envelopeSchema);
            });
        });
        return builder;
    }
}
