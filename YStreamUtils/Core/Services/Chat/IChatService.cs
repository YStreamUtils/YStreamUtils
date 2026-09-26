namespace YStreamUtils.Core.Services.Chat;

public interface IChatService
{
    public Task StartChatStream(string videoId, CancellationToken token = default);
    public void StopChatStream(string videoId);
}