using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Events;

public interface IEventBus
{
    Task PublishAsync(EventKey eventKey, object? payload, CancellationToken cancellationToken = default);

    Action Subscribe(EventKey eventKey, Func<object, CancellationToken, Task> handler);
}