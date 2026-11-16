using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using YStreamUtils.Core.Events;

namespace YStreamUtils.Core.Logging;

public class CustomConsoleFormatter(IServiceProvider serviceProvider) : ConsoleFormatter("HumanReadable")
{
    private readonly Lazy<IEventBus> _eventBus = new(serviceProvider.GetRequiredService<IEventBus>);
    private const string Reset = "\e[0m";
    private const string Gray = "\e[90m";
    private const string White = "\e[97m";

    public override void Write<TState>(
        in LogEntry<TState> logEntry, 
        IExternalScopeProvider? scopeProvider, 
        TextWriter textWriter)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        var category = logEntry.Category ?? string.Empty;
        var lastDot = category.LastIndexOf('.');
        var shortCategory = (lastDot >= 0 && lastDot < category.Length - 1) 
            ? category[(lastDot + 1)..] 
            : category;

        var (levelText, colorCode) = logEntry.LogLevel switch
        {
            LogLevel.Trace       => ("TRC", "\e[37m"),  // White/Light Gray
            LogLevel.Debug       => ("DBG", "\e[36m"),  // Cyan
            LogLevel.Information => ("INF", "\e[32m"),  // Green
            LogLevel.Warning     => ("WRN", "\e[33m"),  // Yellow
            LogLevel.Error       => ("ERR", "\e[31m"),  // Red
            LogLevel.Critical    => ("CRT", "\e[35m"),  // Magenta
            _                    => ("LOG", Reset)
        };

        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        textWriter.Write($"{Gray}[{timestamp}]{Reset}");
        textWriter.Write($"{colorCode}[{levelText}]{Reset}");
        textWriter.Write($"{Gray}[{shortCategory}]{Reset} ");
        textWriter.WriteLine($"{White}{message}{Reset}");

        if (logEntry.Exception != null)
        {
            textWriter.WriteLine($"{colorCode}{logEntry.Exception}{Reset}");
        }
        
        _ = _eventBus.Value.PublishAsync(EventKey.ApplicationLog, $"[{timestamp}][{levelText}][{shortCategory}] {message}");
    }
}
