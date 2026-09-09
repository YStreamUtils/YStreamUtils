using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Acornima.Ast;
using Jint;
using Jint.Native;
using Microsoft.Extensions.Logging;
using YStreamUtils.Core.Bridges;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;

namespace YStreamUtils.Core.Services;
public record CompiledPlugin(Prepared<Script> Program, List<string> Permissions);

public class CompiledScript
{
    public required Prepared<Script> Program { get; init; }
    public required Action Unsubscribe { get; init; }
}

public partial class ScriptsService(
    ILogger<ScriptsService> logger,
    IEventBus eventBus,
    PluginService pluginService)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, CompiledPlugin> _cachedPlugins = new();
    private readonly Dictionary<string, CompiledScript> _cachedScripts = new();
    private readonly ConcurrentDictionary<string, CacheBridge> _caches = new();

    private readonly Dictionary<EventKey, Type> _typeRegistry = new()
    {
        { EventKey.StreamChatMessage, typeof(StreamEventEnvelope<StreamChatMessageEvent>) },
        { EventKey.YoutubeSuperChat, typeof(StreamEventEnvelope<StreamSuperChatMessageEvent>) },
        { EventKey.ManualInvoke, typeof(StreamEventEnvelope<EmptyStruct>) }
    };

    public async Task InitializeVmPoolAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var activePlugins = pluginService.GetActivePlugins();
            _cachedPlugins.Clear();

            foreach (var (name, plugin) in activePlugins)
            {
                try
                {
                    var preparedScript = Engine.PrepareScript(plugin.JavaScriptCode, name);
                    var perms = plugin.Manifest.Permissions.Select(p => p.ToString()).ToList();
                    
                    _cachedPlugins[name] = new CompiledPlugin(preparedScript, perms);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Plugin failed to compile: {PluginName}", name);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public void InjectScopedHostObject(Engine vm, JsObject hostObj, string pluginName, List<string> permissions)
    {
        if (permissions.Contains("network"))
        {
            var fetchBridge = new FetchBridge();
            fetchBridge.Register(vm, hostObj);
        }

        hostObj.Set("log", JsValue.FromObject(vm, new Action<string, string>((level, msg) =>
        {
            logger.Log(ParseLogLevel(level), "[Plugin: {PluginName}] {Message}", pluginName, msg);
        })));
    }

    public async Task RegisterScriptAndBindToBusAsync(EventKey topic, string scriptId, string rawJsString)
    {
        await _lock.WaitAsync();
        try
        {
            var program = Engine.PrepareScript(rawJsString, scriptId);

            if (_cachedScripts.TryGetValue(scriptId, out var existingScript))
            {
                existingScript.Unsubscribe();
                _cachedScripts.Remove(scriptId);
            }

            var matches = PluginsRegex().Matches(rawJsString);
            var detectedPlugins = matches.Select(m => m.Groups[1].Value).Distinct().ToList();

            var unsubscribingAction = eventBus.Subscribe(topic, async (payload, cancellationToken) =>
            {
                var vm = new Engine(options => {
                    options.Strict();
                    options.TimeoutInterval(TimeSpan.FromMilliseconds(150));
                });

                var pluginsObj = new JsObject(vm);
                vm.SetValue("plugins", (JsValue)pluginsObj);

                foreach (var name in detectedPlugins)
                {
                    if (!_cachedPlugins.TryGetValue(name, out var plugin))
                    {
                        logger.LogError("Script tried to use a non-existent plugin: {PluginName}", name);
                        continue;
                    }

                    var pluginHostObj = new JsObject(vm);
                    InjectScopedHostObject(vm, pluginHostObj, name, plugin.Permissions);
                    vm.SetValue("host", (JsValue)pluginHostObj);
                    
                    await vm.EvaluateAsync(plugin.Program, cancellationToken);
                    
                    var boundPluginInstance = vm.GetValue(name);
                    pluginsObj.Set(name, boundPluginInstance);
                }

                var userHost = new JsObject(vm);
                userHost.Set("log", JsValue.FromObject(vm, new Action<string, string>((level, msg) =>
                {
                    logger.Log(ParseLogLevel(level), "[Script: {ScriptId}] {Message}", scriptId, msg);
                })));

                var cacheBridge = _caches.GetOrAdd(scriptId, id => new CacheBridge(id));
                cacheBridge.Register(vm, userHost);

                vm.SetValue("host", (JsValue)userHost);
                vm.SetValue("eventName", topic.ToString());
                vm.SetValue("eventData", JsValue.FromObject(vm, payload));
                vm.SetValue("module", JsValue.Undefined);
                vm.SetValue("exports", JsValue.Undefined);

                try
                {
                    await vm.EvaluateAsync(program, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Script runtime execution crashed. Script: {ScriptId}", scriptId);
                }
                
                await Task.CompletedTask;
            });

            _cachedScripts[scriptId] = new CompiledScript
            {
                Program = program,
                Unsubscribe = unsubscribingAction
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    public string GetMonacoEnvironment(EventKey topic)
    {
        var typeName = "Generic";
        var innerFields = $"    event: \"{topic}\";\n    platform: string;\n    data: any;\n";

        if (_typeRegistry.TryGetValue(topic, out var typeTarget))
        {
            typeName = typeTarget.Name;
            if (typeTarget.IsGenericType)
            {
                var genericArg = typeTarget.GetGenericArguments()[0].Name;
                typeName = $"StreamEventEnvelope_{genericArg}";
            }
            innerFields = $"    event: \"{topic}\";\n    platform: string;\n    data: any;\n";
        }

        var jsPluginDeclarations = pluginService.GetDynamicPluginDefinitions();

        return $$"""

                 {{jsPluginDeclarations}}

                 interface {{typeName}} {
                 {{innerFields}}}

                 declare const eventName: string;
                 declare const eventData: {{typeName}};

                 /**
                  * The standard response returned by YouTube messaging operations.
                  */
                 interface YoutubeReplyResponse {
                   /**
                    * The final operational status.
                    */
                   status: "success" | "error";
                   /**
                    * A descriptive status message or error detail.
                    */
                   message: string;
                 }

                 /**
                  * Interface for interacting with the host's YouTube API context.
                  *
                  * **Plugin Permission:** `youtube` (Only required when running as a plugin)
                  */
                 interface YoutubeContext {
                   /**
                    * Sends a plain message directly to a YouTube Live Chat stream.
                    *
                    * @param liveChatID - The unique ID of the target Live Chat room.
                    * @param message - The raw text message content to transmit.
                    */
                   sendMessage(liveChatID: string, message: string): void;

                   /**
                    * Sends a targeted reply to a specific user within a YouTube Live Chat stream.
                    *
                    * @param liveChatID - The unique ID of the target Live Chat room.
                    * @param authorID - The unique ID of the user being replied to.
                    * @param text - The raw text message content to transmit.
                    * @returns An object containing the operational status and response message.
                    */
                   replyToMessage(
                     liveChatID: string,
                     authorID: string,
                     text: string,
                   ): YoutubeReplyResponse;
                 }

                 /**
                  * Interface for interacting with the host's network layer.
                  *
                  * **Plugin Permission:** `network` (Only required when running as a plugin)
                  */
                 interface HostNetwork {
                   /**
                    * Executes a network request using the host environment's networking stack.
                    *
                    * @param url - The fully-qualified destination URL endpoint.
                    * @param options - Configuration overrides (e.g., headers, body, method).
                    * @returns The raw network response payload.
                    */
                   fetch(url: string, options?: any): any;
                 }

                 interface HostCache {
                   /**
                    * Gets an object from cache (Untyped)
                    * @param key The key of the object you want to get from cache
                    * @returns The value if it is found, null if it is not.
                    */
                   get(key: string): any | null;

                   /**
                    * Gets an object from cache (Typed)
                    * @param key The key of the object you want to get from cache
                    * @returns The value (as T) if it is found, null if it is not
                    */
                   get<T>(key: string): T | null;

                   /**
                    * Adds an object to the cache
                    * @param key The key of the object to cache
                    * @param value The value of the object to cache
                    */
                   set(key: string, value: any): void;

                   /**
                    * Deletes an object from the cache
                    * @param key The key of the item to delete
                    */
                   delete(key: string): void;

                   /**
                    * Deletes the whole cache
                    */
                   clear(): void;
                 }

                 /**
                  * The global object exposed by the script/plugin runtime environment.
                  */
                 declare namespace host {
                   /**
                    * Writes to the host logs.
                    *
                    * @param level - The severity threshold tier (`"debug"`, `"info"`, `"warn"`, `"error"`).
                    * @param msg - The core log description message text.
                    */
                   function log(level: "debug" | "info" | "warn" | "error", msg: string): void;

                   /**
                    * Shared networking capabilities proxy.
                    */
                   const network: HostNetwork;

                   /**
                    * Shared YouTube streaming features context proxy.
                    */
                   const youtube: YoutubeContext;

                   /**
                    * Scoped Cache (based on plugin namespace or script name)
                    */
                   const cache: HostCache;
                 }


                 """;
    }

    private static LogLevel ParseLogLevel(string level) => level.ToLowerInvariant() switch
    {
        "debug" => LogLevel.Debug,
        "warn" => LogLevel.Warning,
        "error" => LogLevel.Error,
        _ => LogLevel.Information
    };
    [GeneratedRegex(@"plugins\.(\w+)")]
    private static partial Regex PluginsRegex();
}
