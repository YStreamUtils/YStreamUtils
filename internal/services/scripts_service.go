package services

import (
	"context"
	"fmt"
	"log/slog"
	"slices"
	"strings"
	"sync"

	"github.com/YStreamUtils/YStreamUtils-Plugin-Registry/ci/types"
	"github.com/dop251/goja"
	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/internal/bridges"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/ports"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
)

type EmptyStruct struct{}

type ScriptCache struct {
	Program     *goja.Program
	Unsubscribe func()
}

type ScriptsService struct {
	BaseService
	ctx            context.Context
	pluginService  *PluginService
	youtubeService *YouTubeService
	vault          ports.SecretVault
	mu             sync.RWMutex
	cachedScripts  map[string]*ScriptCache
	typeRegistry   map[models.EventKey]any
	poolMu         sync.RWMutex
	vmPool         *sync.Pool
	factoryBundle  *goja.Program
}

func NewScriptsService(ctx context.Context, plugins *PluginService, youtubeService *YouTubeService, vault ports.SecretVault) *ScriptsService {
	return &ScriptsService{
		BaseService:    NewBaseService("ScriptsService"),
		ctx:            ctx,
		pluginService:  plugins,
		youtubeService: youtubeService,
		cachedScripts:  make(map[string]*ScriptCache),
		typeRegistry:   make(map[models.EventKey]any),
	}
}

func (ss *ScriptsService) InitializeVMPool() error {
	ss.poolMu.Lock()
	defer ss.poolMu.Unlock()

	activePlugins := ss.pluginService.GetActivePlugins()

	ss.vmPool = &sync.Pool{
		New: func() any {
			vm := goja.New()
			vm.SetFieldNameMapper(goja.TagFieldNameMapper("json", true))

			pluginsObj := vm.NewObject()
			_ = vm.Set("plugins", pluginsObj)

			for _, p := range activePlugins {
				scopedHost := ss.CreateScopedHostObject(vm, p.Name, p.Manifest.Permissions)

				_ = vm.Set("host", scopedHost)

				moduleObj := vm.NewObject()
				moduleExports := vm.NewObject()
				_ = moduleObj.Set("exports", moduleExports)

				_ = vm.Set("module", moduleObj)
				_ = vm.Set("exports", moduleExports)

				_, err := vm.RunString(p.JavaScriptCode)
				if err != nil {
					continue
				}

				finalExports := moduleObj.Get("exports").ToObject(vm)
				pluginInstance := vm.NewObject()

				for _, key := range finalExports.Keys() {
					if key == "__esModule" || key == "default" || key == "host" {
						continue
					}

					rawFunc, ok := goja.AssertFunction(finalExports.Get(key))
					if !ok {
						continue
					}

					boundMethod := vm.ToValue(func(call goja.FunctionCall) goja.Value {
						_ = vm.Set("host", scopedHost)

						res, err := rawFunc(goja.Undefined(), call.Arguments...)
						if err != nil {
							panic(err)
						}
						return res
					})

					_ = pluginInstance.Set(key, boundMethod)
				}

				freezeFunc, _ := goja.AssertFunction(vm.Get("Object").ToObject(vm).Get("freeze"))
				_, _ = freezeFunc(goja.Undefined(), pluginInstance)

				safeNamespaceName := strings.ReplaceAll(p.Name, "-", "_")
				_ = pluginsObj.Set(safeNamespaceName, pluginInstance)
			}

			_ = vm.Set("module", goja.Undefined())
			_ = vm.Set("exports", goja.Undefined())
			_ = vm.Set("host", goja.Undefined())

			freezeFunc, _ := goja.AssertFunction(vm.Get("Object").ToObject(vm).Get("freeze"))
			_, _ = freezeFunc(goja.Undefined(), pluginsObj)

			return vm
		},
	}

	return nil
}

func (ss *ScriptsService) CreateScopedHostObject(vm *goja.Runtime, pluginName string, permissions []types.Permission) *goja.Object {
	hostObj := vm.NewObject()

	authBridge := bridges.NewAuthBridge(ss.vault)
	fetchBridge := bridges.NewFetchBridge()

	authObj := vm.NewObject()
	_ = hostObj.Set("auth", authObj)
	_ = authObj.Set("getAccessToken", func(call goja.FunctionCall) goja.Value {
		return authBridge.GetAccessToken(call, vm, pluginName, permissions)
	})

	networkObj := vm.NewObject()
	_ = hostObj.Set("network", networkObj)
	_ = networkObj.Set("fetch", func(call goja.FunctionCall) goja.Value {
		hasPerm := slices.Contains(permissions, models.PermissionNetwork)

		if !hasPerm {
			panic(vm.NewTypeError(fmt.Sprintf("Security Error: Plugin '%s' lacks 'network' permission.", pluginName)))
		}

		return fetchBridge.Fetch(call, vm)
	})

	youtubeObj := vm.NewObject()
	_ = hostObj.Set("youtube", youtubeObj)

	_ = youtubeObj.Set("replyToMessage", func(call goja.FunctionCall) goja.Value {
		hasPerm := slices.Contains(permissions, models.PermissionYoutube)
		if !hasPerm {
			panic(vm.NewTypeError(fmt.Sprintf("Security Error: Plugin '%s' lacks 'youtube' permissions.", pluginName)))
		}

		liveChatID := call.Argument(0).String()
		authorID := call.Argument(1).String()
		replyText := call.Argument(2).String()

		err := ss.youtubeService.SendChannelReply(liveChatID, authorID, replyText)
		if err != nil {
			return vm.ToValue(map[string]interface{}{
				"status":  "error",
				"message": err.Error(),
			})
		}

		return vm.ToValue(map[string]interface{}{
			"status":  "success",
			"message": "Reply delivered successfully to YouTube live chat room.",
		})
	})

	return hostObj
}

func (ss *ScriptsService) RegisterScriptAndBindToBus(topic models.EventKey, scriptID string, rawJsString string) error {
	log := ss.Logger.With("scriptId", scriptID, "topic", topic)

	ss.mu.Lock()
	defer ss.mu.Unlock()

	program, err := goja.Compile(scriptID, rawJsString, true)
	if err != nil {
		return fmt.Errorf("javascript compilation error: %w", err)
	}

	if val, exists := ss.cachedScripts[scriptID]; exists {
		val.Unsubscribe()
		delete(ss.cachedScripts, scriptID)
	}

	unsub := application.Get().Event.On(string(topic), func(customEvent *application.CustomEvent) {
		ss.poolMu.RLock()
		pool := ss.vmPool
		ss.poolMu.RUnlock()

		if pool == nil {
			return
		}

		vm := pool.Get().(*goja.Runtime)

		_ = vm.Set("event", customEvent)

		userHost := vm.Get("host")
		if userHost == nil || goja.IsUndefined(userHost) {
			userHost = vm.NewObject()
			_ = vm.Set("host", userHost)
		}
		_ = userHost.ToObject(vm).Set("log", func(level string, msg string) {
			logLevel := utils.ParseLogLevel(level, slog.LevelInfo)
			log.Log(context.Background(), logLevel, msg)
		})

		if _, err = vm.RunProgram(program); err != nil {
			log.Error("script runtime execution crashed", "error", err.Error())
		}

		_ = vm.Set("event", goja.Undefined())
		pool.Put(vm)
	})

	ss.cachedScripts[scriptID] = &ScriptCache{
		Program:     program,
		Unsubscribe: unsub,
	}

	return nil
}

func (ss *ScriptsService) GetDynamicPluginDefinitions() (string, error) {
	var sb strings.Builder

	sb.WriteString("declare namespace plugins {\n")

	activePlugins := ss.pluginService.GetActivePlugins()

	for _, p := range activePlugins {
		safeNamespaceName := strings.ReplaceAll(p.Name, "-", "_")
		fmt.Fprintf(&sb, "    namespace %s {\n", safeNamespaceName)

		if p.TypeScriptDefs != "" {
			cleanedDefs := strings.ReplaceAll(p.TypeScriptDefs, "export ", "")
			cleanedDefs = strings.ReplaceAll(cleanedDefs, "declare ", "")

			cleanedDefs = strings.ReplaceAll(cleanedDefs, "const host: HostContext;", "")
			cleanedDefs = strings.ReplaceAll(cleanedDefs, "const host:HostContext;", "")

			indentedDefs := strings.ReplaceAll(cleanedDefs, "\n", "\n        ")
			sb.WriteString("        ")
			sb.WriteString(strings.TrimSpace(indentedDefs))
			sb.WriteString("\n")
		} else {
			for _, fn := range p.Functions {
				fmt.Fprintf(&sb, "        function %s(...args: any[]): any;\n", fn)
			}
		}
		sb.WriteString("    }\n\n")
	}

	sb.WriteString("}\n")
	return sb.String(), nil
}

func (ss *ScriptsService) GetMonacoEnvironment(topic string) (string, error) {
	eventKey := models.EventKey(topic)
	provider, exists := ss.typeRegistry[eventKey]

	typeName := "Generic"
	innerFields := fmt.Sprintf("    event: \"%s\";\n    platform: string;\n    data: any;\n", topic)

	if exists {
		rawType := fmt.Sprintf("%T", provider)
		if idx := strings.LastIndex(rawType, "."); idx != -1 {
			typeName = rawType[idx+1:]
		} else {
			typeName = rawType
		}

		typeName = strings.ReplaceAll(typeName, "StreamEventEnvelope", "")
		typeName = strings.ReplaceAll(typeName, "[", "")
		typeName = strings.ReplaceAll(typeName, "]", "")

		innerFields = utils.GenerateTSFields(provider, string(eventKey))
	}

	jsPluginDeclarations, _ := ss.GetDynamicPluginDefinitions()

	fullEnvironment := fmt.Sprintf(`
%s

interface %s {
%s}

interface WailsEvent {
    id: string;
    name: string;
    sender: string;
    data: %s;
}

declare const event: WailsEvent;

interface YoutubeReplyResponse {
    status: "success" | "error";
    message: string;
}

interface YoutubeHostNamespace {
    replyToMessage(liveChatID: string, authorID: string, text: string): YoutubeReplyResponse;
}

declare namespace host {
    function log(level: "debug" | "info" | "warn" | "error" | string, msg: string): void;
		const youtube: YoutubeHostNamespace;
}
`, jsPluginDeclarations, typeName, innerFields, typeName)

	return fullEnvironment, nil
}

func (ss *ScriptsService) ServiceStartup(ctx context.Context, options application.ServiceOptions) error {
	ss.typeRegistry[models.EventKeyStreamChatMessage] = models.StreamEventEnvelope[models.StreamChatMessageEvent]{}
	ss.typeRegistry[models.EventKeyYoutubeSuperchat] = models.StreamEventEnvelope[models.StreamSuperchatMessageEvent]{}
	ss.typeRegistry[models.EventKeyManualInvoke] = models.StreamEventEnvelope[EmptyStruct]{}

	_ = ss.InitializeVMPool()

	ss.pluginService.StartDevPluginWatcher(func() {
		if err := ss.InitializeVMPool(); err != nil {
			slog.Error("Rebuilding pool failed during hot reload", "error", err)
		} else {
			slog.Info("Script engine pool completely refreshed with new changes")
		}
	})

	// WHAT DO YOU MEAN JSON.stringify DOESN'T CAPTURE FUNCTIONS?????????????
	// ss.RegisterScriptAndBindToBus(
	// 	models.EventKeyManualInvoke,
	// 	"test_script",
	// 	`host.log("info", JSON.stringify({
	//       string_utils: Object.keys(plugins.string_utils)
	//   }))`,
	// )
	// application.Get().Event.Emit(string(models.EventKeyManualInvoke))
	return nil
}
