using Microsoft.Extensions.Logging;

namespace YStreamUtils.Core.Bridges;

public class LoggerBridge(ILogger logger)
{
    public void LogInformation(string message) => logger.LogInformation(message);
    
    public void LogWarning(string message) => logger.LogWarning(message);
    
    public void LogError(string message) => logger.LogError(message);
    
    public void LogCritical(string message) => logger.LogCritical(message);
    
    public void Log(LogLevel level, string message) => logger.Log(level, message);
    
}
