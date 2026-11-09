using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace YStreamUtils.Core.Events;

public class EventBus(ILogger<EventBus> logger) : IEventBus
{
    private readonly ConcurrentDictionary<EventKey, List<Func<object, CancellationToken, Task>>> _handlers = new();
    
    private readonly List<Func<object, CancellationToken, Task>> _superHandlers = [];
    private readonly Lock _superHandlersLock = new();

    private readonly ConcurrentDictionary<Delegate, Func<object, CancellationToken, Task>> _genericWrapperMap = new();

    public Action Subscribe(EventKey eventKey, Func<object, CancellationToken, Task> handler)
    {
        if (eventKey == EventKey.All)
        {
            lock (_superHandlersLock)
            {
                _superHandlers.Add(handler);
            }
            return () => { lock (_superHandlersLock) { _superHandlers.Remove(handler); } };
        }

        var currentHandlers = _handlers.GetOrAdd(eventKey, _ => []);
        
        lock (currentHandlers)
        {
            currentHandlers.Add(handler);
        }

        return () =>
        {
            if (!_handlers.TryGetValue(eventKey, out var handlers)) return;
            
            lock (handlers)
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _handlers.TryRemove(eventKey, out _);
                }
            }
        };
    }

    public Action Subscribe<T>(EventKey eventKey, Func<T, CancellationToken, Task> handler)
    {
        Func<object, CancellationToken, Task> wrappedHandler = (payload, token) => handler((T)payload, token);
        _genericWrapperMap[handler] = wrappedHandler;

        var unsubscribeAction = Subscribe(eventKey, wrappedHandler);

        return () =>
        {
            unsubscribeAction();
            _genericWrapperMap.TryRemove(handler, out _);
        };
    }

    public async Task PublishAsync(EventKey eventKey, object? payload, CancellationToken cancellationToken = default)
    {
        var fallbackPayload = payload ?? new EmptyStruct();
        List<Task> tasksToAwait = [];

        List<Func<object, CancellationToken, Task>> superSnapshot;
        lock (_superHandlersLock)
        {
            superSnapshot = [.. _superHandlers];
        }
        tasksToAwait.AddRange(superSnapshot.Select(handler => handler(fallbackPayload, cancellationToken)));

        if (_handlers.TryGetValue(eventKey, out var handlers))
        {
            List<Func<object, CancellationToken, Task>> regularSnapshot;
            lock (handlers)
            {
                regularSnapshot = [.. handlers];
            }
            tasksToAwait.AddRange(regularSnapshot.Select(handler => handler(fallbackPayload, cancellationToken)));
        }

        if (tasksToAwait.Count > 0)
        {
            try
            {
                await Task.WhenAll(tasksToAwait);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while publishing event {EventKey}", eventKey);
                throw;
            }
        }
    }
}
