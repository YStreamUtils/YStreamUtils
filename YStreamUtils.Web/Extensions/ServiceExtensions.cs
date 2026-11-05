using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Logging;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;
using YStreamUtils.Core.Services.Auth;
using YStreamUtils.Core.Services.Chat;
using YStreamUtils.Core.Services.Metrics;
using YStreamUtils.Core.Services.Profile;
using YStreamUtils.Core.Services.Stream;
using YStreamUtils.Core.Services.YouTube;

namespace YStreamUtils.Web.Extensions;

public static class ServiceExtensions
{
    public static WebApplicationBuilder AddYStreamUtils(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => { options.FormatterName = "HumanReadable"; });

        builder.Logging.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();

        var serviceCollection = builder.Services;
        serviceCollection.AddHttpClient();

        serviceCollection.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
        });

        builder.Services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer((schema, ctx, _) =>
            {
                var type = ctx.JsonTypeInfo.Type;
                if (type.IsGenericType)
                {
                    schema.Title = $"{type.Name.Split('`')[0]}_{type.GetGenericArguments()[0].Name}";
                }

                if (ctx.JsonTypeInfo.Type.Name == nameof(EventKey) || ctx.JsonTypeInfo.Type == typeof(EventKey))
                {
                    schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
                    schema.Properties.Clear();

                    schema.AllOf ??= new List<IOpenApiSchema>();
                    schema.AllOf.Clear();

                    schema.Type = JsonSchemaType.String;

                    schema.Enum ??= new List<JsonNode>();
                    schema.Enum.Clear();

                    foreach (var key in EventKey.KnownKeys)
                    {
                        schema.Enum?.Add(JsonValue.Create(key.ToString()));
                    }

                    schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                    schema.Extensions?["x-speakeasy-unknown-values"] = new JsonNodeExtension("allow");
                }

                return Task.CompletedTask;
            });

            options.AddDocumentTransformer(async (document, context, cancellationToken) =>
            {
                var knownTypes = Events.KnownTypeRegistry.Select(x => x.Value);

                foreach (var type in knownTypes)
                {
                    var schemaId = type.IsGenericType
                        ? type.GetGenericArguments()[0].Name
                        : type.Name;

                    document.Components ??= new OpenApiComponents();
                    document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
                    if (document.Components.Schemas.ContainsKey(schemaId)) continue;
                    var generatedSchema =
                        await context.GetOrCreateSchemaAsync(type, cancellationToken: cancellationToken);
                    document.Components ??= new OpenApiComponents();
                    document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
                    document.Components.Schemas.Add(schemaId, generatedSchema);
                }
            });
        });

        serviceCollection.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
        });

        builder.Services.AddDbContextFactory<SqliteDbContext>();

        builder.Services.AddSingleton<IDbContextFactory<AppDbContext>>(sp =>
            new DbContextFactoryBridge<SqliteDbContext>(
                sp.GetRequiredService<IDbContextFactory<SqliteDbContext>>()));


        serviceCollection.AddSingleton<IEventBus, EventBus>();
        serviceCollection.AddSingleton<PluginService>();
        serviceCollection.AddSingleton<ScriptsService>();
        serviceCollection.AddSingleton<SettingsService>();

        serviceCollection.AddSingleton<StreamPollingService>();
        serviceCollection.AddSingleton<StreamMonitorService>();
        serviceCollection.AddKeyedSingleton<IMetricsService, YouTubeMetricsService>(Platform.YouTube);

        serviceCollection.AddKeyedSingleton<IChatService, YouTubeChatService>(Platform.YouTube);

        serviceCollection.AddKeyedSingleton<IProfileService, YouTubeProfileService>(Platform.YouTube);

        serviceCollection.AddKeyedSingleton<IStreamFinderService, YouTubeStreamService>(Platform.YouTube);
        
        serviceCollection.AddKeyedSingleton<IAuthService, YouTubeAuthService>(Platform.YouTube);

        serviceCollection.AddSingleton<IDataStore, DbDataStore>();
        serviceCollection.AddSingleton<YouTubeCredentialService>();
        return builder;
    }
}