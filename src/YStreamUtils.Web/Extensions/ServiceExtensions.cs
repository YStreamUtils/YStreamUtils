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