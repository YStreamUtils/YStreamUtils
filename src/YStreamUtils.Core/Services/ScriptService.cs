using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YScriptEngine.Abstractions;
using YScriptEngine.Roslyn;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services;

public class ScriptsService(
    RoslynScriptCompiler compiler,
    ILogger<ScriptsService> logger,
    IEventBus eventBus,
    PluginService pluginService,
    IServiceProvider serviceProvider,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, Action> _activeSubscriptions = new();

    public async Task RegisterScriptAndBindToBusAsync(string topic, string scriptId, string source)
    {
        await _lock.WaitAsync();
        try
        {
            if (_activeSubscriptions.TryGetValue(scriptId, out var unsubscribe))
            {
                unsubscribe();
                _activeSubscriptions.Remove(scriptId);
            }

            if (!Events.Events.KnownTypeRegistry.TryGetValue(topic, out var targetContextType))
            {
                logger.LogError("No registered context mapping envelope found for event topic: {Topic}", topic);
                return;
            }

            var scriptContextType = typeof(ScriptContext<>).MakeGenericType(targetContextType);
            var compiledScript = await compiler.CompileAsync(source, scriptContextType);

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existing = await dbContext.UserScripts
                .FirstOrDefaultAsync(s => s.ScriptId == scriptId);

            if (existing is null)
            {
                dbContext.UserScripts.Add(new UserScript
                {
                    ScriptId = scriptId,
                    Source = source,
                    Topic = topic
                });
            }
            else
            {
                existing.Source = source;
                existing.Topic = topic;
            }

            await dbContext.SaveChangesAsync();
            
            var eventNameString = topic;
            
            var unsubscribeAction = eventBus.Subscribe(topic, async (payload, _) =>
            {
                var contextInstance = CreateTypedContext((dynamic)payload, scriptId, eventNameString, serviceProvider, pluginService.GetPluginServiceProvider());
                
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

    private static ScriptContext<TPayload> CreateTypedContext<TPayload>(
        TPayload payload,
        string scriptName,
        string eventName,
        IServiceProvider hostServices, 
        IServiceProvider plugins)
    {
        return new ScriptContext<TPayload>(scriptName, eventName, payload, plugins, hostServices);
    }
}
