using System.Reflection;
using System.Text.Json;
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
        
        serviceCollection.AddOpenApi(options =>
        {
            var eventTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IStreamEventData).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
                .ToList();

            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type != typeof(IStreamEventData)) return Task.CompletedTask;
                
                schema.OneOf = eventTypes
                    .Where(type => !string.IsNullOrEmpty(type.Name))
                    .Select(IOpenApiSchema (type) => new OpenApiSchemaReference($"StreamEventEnvelopeOf{type.Name}"))
                    .ToList();

                schema.Type = null;
                return Task.CompletedTask;
            });

            options.AddDocumentTransformer(async (document, context, cancellationToken) =>
            {
                if (document.Components?.Schemas == null) return;

                var mandatoryTypes = new List<Type> { typeof(BaseUserData), typeof(EmptyStruct) }
                    .Concat(eventTypes);

                foreach (var type in mandatoryTypes)
                {
                    if (string.IsNullOrEmpty(type.Name) || document.Components.Schemas.ContainsKey(type.Name)) continue;

                    var underlyingSchema = await context.GetOrCreateSchemaAsync(type, null, cancellationToken);
                    document.Components.Schemas.Add(type.Name, underlyingSchema);
                }

                foreach (var type in eventTypes)
                {
                    if (string.IsNullOrEmpty(type.Name)) continue;

                    var envelopeSchemaName = $"StreamEventEnvelopeOf{type.Name}";
                    if (document.Components.Schemas.ContainsKey(envelopeSchemaName)) continue;

                    var properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["tenantId"] = new OpenApiSchema { Type = JsonSchemaType.String },
                        ["platform"] = new OpenApiSchemaReference("Platform"),
                        ["timestamp"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null, Format = "date-time" },
                        ["data"] = new OpenApiSchemaReference(type.Name)
                    };

                    var envelopeSchema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = properties,
                        Required = new HashSet<string> { "tenantId" }
                    };

                    document.Components.Schemas.Add(envelopeSchemaName, envelopeSchema);
                }
            });
        });
        return builder;
    }
}
