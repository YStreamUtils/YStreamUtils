using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace YStreamUtils.Core.Logging;

public class CustomConsoleFormatter() : ConsoleFormatter("HumanReadable")
{
    private const string Reset = "\e[0m";
    private const string Gray = "\e[1;30m";
    private const string White = "\e[1;37m";

    public override void Write<TState>(
        in LogEntry<TState> logEntry, 
        IExternalScopeProvider? scopeProvider, 
        TextWriter textWriter)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var category = logEntry.Category;
        var shortCategory = category.Contains('.') ? category[category.LastIndexOf('.')..][1..] : category;

        var (levelText, colorCode) = logEntry.LogLevel switch
        {
            LogLevel.Trace => ("TRC", "\e[37m"),      
            LogLevel.Debug => ("DBG", "\e[36m"),      
            LogLevel.Information => ("INF", "\e[32m"),
            LogLevel.Warning => ("WRN", "\e[33m"),    
            LogLevel.Error => ("ERR", "\e[31m"),      
            LogLevel.Critical => ("CRT", "\e[1;35m"), 
            _ => ("LOG", Reset)
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
    }
}
