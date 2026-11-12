using Microsoft.Extensions.Logging;
using YScriptEngine.Abstractions;
using YScriptEngine.Roslyn;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services;

public partial class ScriptsService(
    RoslynCompiler compiler,
    ILogger<ScriptsService> logger,
    IEventBus eventBus,
    PluginService pluginService,
    IServiceProvider serviceProvider)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, Action> _activeSubscriptions = new();

    public async Task RegisterScriptAndBindToBusAsync(EventKey topic, string scriptId, string source)
    {
        await _lock.WaitAsync();
        try
        {
            if (_activeSubscriptions.TryGetValue(scriptId, out var unsubscribe))
            {
                unsubscribe();
                _activeSubscriptions.Remove(scriptId);
            }

            if (Events.Events.KnownTypeRegistry.TryGetValue(topic, out var targetContextType))
            {
                logger.LogError("No registered context mapping envelope found for event topic: {Topic}", topic);
                return;
            }

            var compiledScript = await compiler.CompileAsync(source, targetContextType!);

            var eventNameString = topic.ToString();
            
            var unsubscribeAction = eventBus.Subscribe(topic, async (payload, _) =>
            {
                var contextInstance = CreateTypedContext((dynamic)payload, eventNameString, serviceProvider, serviceProvider);
                
                try
                {
                    await compiledScript.ExecuteAsync((IScriptContext)contextInstance);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Script runtime execution crashed. Script: {ScriptId}", scriptId);
                }
            });

            _activeSubscriptions[scriptId] = unsubscribeAction;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string Lower(string s) => char.ToLowerInvariant(s[0]) + s[1..];
    
    private static IScriptContext CreateTypedContext<TPayload>(
        TPayload payload,
        string eventName,
        IServiceProvider hostServices, 
        IServiceProvider plugins)
    {
        return new ScriptContext<TPayload>(eventName, payload, plugins, hostServices);
    }
}
