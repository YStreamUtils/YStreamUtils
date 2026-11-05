namespace YStreamUtils.Core.Services.Metrics;

public record struct StreamMetrics(string ViewerCount);
public interface IMetricsService
{
    public Task<StreamMetrics> GetMetrics(string videoId, CancellationToken token = default);
}