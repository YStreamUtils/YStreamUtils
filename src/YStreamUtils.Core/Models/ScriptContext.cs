using YScriptEngine.Abstractions;

namespace YStreamUtils.Core.Models;

public class ScriptContext<T>(
    string eventName,
    T eventData,
    IServiceProvider plugins,
    IServiceProvider services)
    : IScriptContext
{
    public string EventName { get; init; } = eventName;

    public T EventData { get; init; } = eventData;

    public IServiceProvider Plugins { get; init; } = plugins;
    public IServiceProvider Services { get; init; } = services;
}