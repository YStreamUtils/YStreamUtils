namespace YStreamUtils.Core.Events;

public interface IEventBus
{
    Task PublishAsync(EventKey eventKey, object? payload, CancellationToken cancellationToken = default);

    Action Subscribe(EventKey eventKey, Func<object, CancellationToken, Task> handler);
    
    Action Subscribe<T>(EventKey eventKey, Func<T, CancellationToken, Task> handler);
}