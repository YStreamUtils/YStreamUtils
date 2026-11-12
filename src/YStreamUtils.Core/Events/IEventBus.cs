namespace YStreamUtils.Core.Events;

public interface IEventBus
{
    Task PublishAsync(string eventKey, object? payload, CancellationToken cancellationToken = default);

    Action Subscribe(string eventKey, Func<object, CancellationToken, Task> handler);
    
    Action Subscribe<T>(string eventKey, Func<T, CancellationToken, Task> handler);
}