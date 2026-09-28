using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Jint;
using Jint.Runtime.Interop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YScriptEngine.Abstractions;
using YScriptEngine.Jint;
using YScriptEngine.Jint.Generators;
using YStreamUtils.Core.Bridges;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Script;

namespace YStreamUtils.Core.Services;

public partial class ScriptsService(
    ILogger<ScriptsService> logger,
    IEventBus eventBus,
    PluginService pluginService,
    IServiceProvider serviceProvider)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, Action> _activeSubscriptions = new();
    private readonly ConcurrentDictionary<string, CacheBridge> _caches = new();

    public async Task RegisterScriptAndBindToBusAsync(EventKey topic, string scriptId, string rawJsString)
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
            var engine = new Engine(options =>
            {
                options.SetTypeResolver(new TypeResolver
                {
                    MemberNameComparer = StringComparer.OrdinalIgnoreCase
                    
                });
                options.AllowClr();
            });
            var compiler = new JintCompiler(engine);

            var compiledScript = await compiler.CompileAsync(rawJsString, targetContextType);

            var matches = PluginsRegex().Matches(rawJsString);
            var detectedPlugins = matches.Select(m => m.Groups[1].Value).Distinct().ToList();
            logger.LogDebug("Found {DetectedPluginsCount} plugins for Script {ScriptId}", detectedPlugins.Count,
                scriptId);

            var activePlugins = pluginService.GetActivePlugins();
            var staticPluginsTemplate = new Dictionary<string, object>();

            foreach (var name in detectedPlugins)
            {
                if (activePlugins.TryGetValue(name, out var plugin))
                {
                    staticPluginsTemplate[Lower(name)] = plugin.PluginInstance;
                }
                else
                {
                    logger.LogError("Script registration warning: Missing plugin: {PluginName}", name);
                }
            }

            var eventNameString = topic.ToString();
            
            var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            var cacheBridge = _caches.GetOrAdd(scriptId, id =>
            {
                using var context = contextFactory.CreateDbContext();
                return new CacheBridge(id, context);
            });

            
            var unsubscribeAction = eventBus.Subscribe(topic, async (payload, _) =>
            {
                var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                
                var contextInstance = CreateTypedContext((dynamic)payload, scriptId, eventNameString, cacheBridge, httpClient, logger);

                if (contextInstance is IJintScriptContext scriptContext)
                {
                    foreach (var kvp in staticPluginsTemplate)
                    {
                        scriptContext.Plugins[kvp.Key] = kvp.Value;
                    }
                }

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
    
    [GeneratedRegex(@"plugins\.(\w+)")]
    private static partial Regex PluginsRegex();

    public string GetMonacoEnvironment(EventKey topic)
    {
        if (Events.Events.KnownTypeRegistry.TryGetValue(topic, out var targetContextType)) targetContextType = typeof(object);
        var closedContextType = typeof(ScriptContext<>).MakeGenericType(targetContextType ?? throw new InvalidOperationException("Target context type is null... somehow?"));

        var pluginsDts = pluginService.GetDynamicPluginDefinitions();
        var contextDts = JintCmSchemaGenerator.GenerateContextVariablesSchema(closedContextType);

        return $"{pluginsDts}\n{contextDts}";
    }


    private static string Lower(string s) => char.ToLowerInvariant(s[0]) + s[1..];
    
    private static IScriptContext CreateTypedContext<TPayload>(
        TPayload payload,
        string scriptName,
        string eventName, 
        CacheBridge cache, 
        HttpClient client, 
        ILogger logger)
    {
        return new ScriptContext<TPayload>(scriptName, eventName, payload, cache, client, logger);
    }
}
