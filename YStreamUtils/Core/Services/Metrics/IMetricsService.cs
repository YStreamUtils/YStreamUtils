namespace YStreamUtils.Core.Services.Metrics;

public interface IMetricsService
{
    public Task StartMetricStream(string videoId, CancellationToken token = default);
    public Task StopMetricStream(string videoId);
}