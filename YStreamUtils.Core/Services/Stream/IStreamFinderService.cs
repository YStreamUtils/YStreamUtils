namespace YStreamUtils.Core.Services.Stream;

public interface IStreamFinderService
{
    public Task<List<StreamInfo>> GetActiveBroadcastAsync(CancellationToken token = default);
}