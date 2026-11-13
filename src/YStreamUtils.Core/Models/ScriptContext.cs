using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YScriptEngine.Abstractions;

namespace YStreamUtils.Core.Models;

public class ScriptContext<T>(
    string scriptName,
    string eventName,
    T eventData,
    IServiceProvider plugins,
    IServiceProvider services)
    : IScriptContext
{
    public string EventName { get; init; } = eventName;

    public T EventData { get; init; } = eventData;

    public IServiceProvider Plugins { get; init; } = plugins;
    public TPlugin GetPlugin<TPlugin>() where TPlugin : notnull => Plugins.GetRequiredService<TPlugin>();
    public object GetPlugin(Type type) => Plugins.GetRequiredService(type);
    
    public IServiceProvider Services { get; init; } = services;
    public ILogger Logger => Services.GetRequiredService<ILoggerFactory>().CreateLogger(scriptName);
}