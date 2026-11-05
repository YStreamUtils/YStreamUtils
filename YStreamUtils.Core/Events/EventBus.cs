using System.Collections.Concurrent;

namespace YStreamUtils.Core.Events;

public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<EventKey, List<Func<object, CancellationToken, Task>>> _handlers = new();
    private readonly List<Func<object, CancellationToken, Task>> _superHandlers = [];

    public Action Subscribe(EventKey eventKey, Func<object, CancellationToken, Task> handler)
    {
        if (eventKey == EventKey.All)
        {
            _superHandlers.Add(handler);
            return () => _superHandlers.Remove(handler);
        }
        
        
        _handlers.AddOrUpdate(
            eventKey,
            _ => [handler],
            (_, currentHandlers) =>
            {
                lock (currentHandlers) { currentHandlers.Add(handler); }
                return currentHandlers;
            });

        return () =>
        {
            if (!_handlers.TryGetValue(eventKey, out var currentHandlers)) return;
            lock (currentHandlers) { currentHandlers.Remove(handler); }
        };
    }
    
    public Action Subscribe<T>(EventKey eventKey, Func<T, CancellationToken, Task> handler)
    {
        return Subscribe(eventKey, WrappedHandler);

        Task WrappedHandler(object payload, CancellationToken token) => handler((T)payload, token);
    }

    public async Task PublishAsync(EventKey eventKey, object? payload, CancellationToken cancellationToken = default)
    {
        
        var superHandlersTasks = Task.WhenAll(_superHandlers.Select(x => x(payload, cancellationToken)));
        var regularHandlersTasks = Task.Run(() =>
        {
            if (!_handlers.TryGetValue(eventKey, out var handlers)) return new List<Task>();

            List<Func<object, CancellationToken, Task>> handlersToExecute;
            lock (handlers)
            {
                handlersToExecute = [.. handlers];
            }
            
            return handlersToExecute.Select(handler => handler(payload ?? new EmptyStruct(), cancellationToken));
        }, cancellationToken);
        

        await Task.WhenAll(superHandlersTasks, regularHandlersTasks);
    }
}