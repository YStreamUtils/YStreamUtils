using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services.Stream;

namespace YStreamUtils.Core.Services;

public class StreamPollingService(
    ILogger<StreamPollingService> logger,
    IDbContextFactory<AppDbContext> contextFactory,
    IServiceProvider serviceProvider,
    StreamMonitorService monitorService) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Stream Polling Service is starting.");

        using var timer = new PeriodicTimer(_pollingInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    logger.LogDebug("Polling for stream updates...");

                    var context = await contextFactory.CreateDbContextAsync(stoppingToken);
                    var configs = await context.OAuthConfigs.AsNoTracking().ToListAsync(stoppingToken);
                    
                    var allActiveStreams = new List<StreamInfo>();
                    foreach (var config in configs)
                    {
                        var platformStreams = await DoPollingWorkAsync(config.Platform, stoppingToken);
                        allActiveStreams.AddRange(platformStreams);
                    }
                    
                    monitorService.UpdateActiveStreams(allActiveStreams);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while polling streams.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Stream Polling Service is stopping due to cancellation request.");
        }
    }

    private async Task<List<StreamInfo>> DoPollingWorkAsync(Platform platform, CancellationToken cancellationToken)
    {
        var metricsService = serviceProvider.GetRequiredKeyedService<IStreamFinderService>(platform);
        return await metricsService.GetActiveBroadcastAsync(cancellationToken);
    }
}