using System.Collections.Concurrent;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Events;

public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<EventKey, List<Func<object, CancellationToken, Task>>> _handlers = new();

    public Action Subscribe(EventKey eventKey, Func<object, CancellationToken, Task> handler)
    {
        _handlers.AddOrUpdate(
            eventKey,
            _ => [handler],
            (_, currentHandlers) =>
            {
                lock (currentHandlers)
                {
                    currentHandlers.Add(handler);
                }
                return currentHandlers;
            });

        return () =>
        {
            if (!_handlers.TryGetValue(eventKey, out var currentHandlers)) return;
            lock (currentHandlers)
            {
                currentHandlers.Remove(handler);
            }
        };
    }

    public async Task PublishAsync(EventKey eventKey, object? payload, CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(eventKey, out var handlers))
        {
            return;
        }

        List<Func<object, CancellationToken, Task>> handlersToExecute;
        lock (handlers)
        {
            handlersToExecute = [.. handlers];
        }

        var tasks = handlersToExecute
            .Select(handler => handler(payload ?? new EmptyStruct(), cancellationToken));

        await Task.WhenAll(tasks);
    }
}