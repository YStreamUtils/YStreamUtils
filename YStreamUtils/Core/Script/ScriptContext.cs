using Microsoft.Extensions.Logging;
using YScriptEngine.Abstractions;
using YStreamUtils.Core.Bridges;

namespace YStreamUtils.Core.Script;

public class ScriptContext<T>(
    string scriptName,
    string eventName,
    T eventData,
    CacheBridge cache,
    HttpClient network,
    ILogger log)
    : IJintScriptContext
{
    public string EventName { get; init; } = eventName;

    public T EventData { get; init; } = eventData;

    public NetworkBridge Network { get; init; } = new(network);
    
    public LoggerBridge Logger { get; init; } = new(scriptName, log);
    public CacheBridge Cache { get; init; } = cache;
    public Dictionary<string, object> Plugins { get; init; } = new();

}

public class LoggerBridge(string scriptName, ILogger logger)
{
    public void LogInformation(string message) => logger.LogInformation("<{ScriptName}> {Message}", scriptName, message);
    
    public void LogWarning(string message) => logger.LogWarning("<{ScriptName}> {Message}", scriptName, message);
    
    public void LogError(string message) => logger.LogError("<{ScriptName}> {Message}", scriptName, message);
    
    public void LogCritical(string message) => logger.LogCritical("<{ScriptName}> {Message}", scriptName, message);
    
    public void Log(LogLevel level, string message) => logger.Log(level, "<{ScriptName}> {Message}", scriptName, message);
}

public interface IJintScriptContext : IScriptContext
{
    public NetworkBridge Network { get; init; }
    public LoggerBridge Logger { get; init; }
    public CacheBridge Cache { get; init; }
    public Dictionary<string, object> Plugins { get; init; }
}