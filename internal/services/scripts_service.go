package services

import (
	"context"
	"fmt"
	"log/slog"
	"regexp"
	"strings"
	"sync"

	"github.com/YStreamUtils/YStreamUtils-Plugin-Registry/ci/types"
	"github.com/dop251/goja"
	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/ports"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
)

type EmptyStruct struct{}

type CompiledPlugin struct {
	Name        string
	Program     *goja.Program
	Permissions []types.Permission
}

type CompiledScript struct {
	Program     *goja.Program
	Unsubscribe func()
}

type ScriptsService struct {
	BaseService
	ctx            context.Context
	pluginService  *PluginService
	youtubeService *YouTubeService
	vault          ports.SecretVault

	mu            sync.RWMutex
	typeRegistry  map[models.EventKey]any
	cachedScripts map[string]*CompiledScript
	cachedPlugins map[string]CompiledPlugin
}

func NewScriptsService(ctx context.Context, plugins *PluginService, youtubeService *YouTubeService, vault ports.SecretVault) *ScriptsService {
	return &ScriptsService{
		BaseService:    NewBaseService("ScriptsService"),
		ctx:            ctx,
		pluginService:  plugins,
		youtubeService: youtubeService,
		cachedScripts:  make(map[string]*CompiledScript),
		typeRegistry:   make(map[models.EventKey]any),
	}
}

func (ss *ScriptsService) InitializeVMPool() error {
	ss.mu.Lock()
	defer ss.mu.Unlock()

	activePlugins := ss.pluginService.GetActivePlugins()
	ss.cachedPlugins = make(map[string]CompiledPlugin, len(activePlugins))

	for _, p := range activePlugins {
		prg, err := goja.Compile(p.Name, p.JavaScriptCode, true)
		if err != nil {
			ss.Logger.Error("plugin failed to compile", "plugin", p.Name)
			continue
		}

		perms := make([]types.Permission, 0, len(p.Manifest.Permissions))
		for _, perm := range p.Manifest.Permissions {
			perms = append(perms, perm)
		}

		ss.cachedPlugins[p.Name] = CompiledPlugin{
			Name:        p.Name,
			Program:     prg,
			Permissions: perms,
		}
	}

	return nil
}

func (ss *ScriptsService) CreateScopedHostObject(vm *goja.Runtime, pluginName string, permissions []types.Permission) *goja.Object {
	hostObj := vm.NewObject()

	permMap := make(map[types.Permission]bool, len(permissions))
	for _, perm := range permissions {
		permMap[perm] = true
	}

	if permMap[models.PermissionNetwork] {
		networkObj := vm.NewObject()
		_ = networkObj.Set("fetch", func(url string, options goja.Value) goja.Value {
			ss.Logger.Info("Plugin triggering outbound HTTP fetch", "plugin", pluginName, "url", url)
			return ss.responseObjectFactory(vm, url)
		})
		_ = hostObj.Set("network", networkObj)
	}

	_ = hostObj.Set("log", func(level string, msg string) {
		logLevel := utils.ParseLogLevel(level, slog.LevelInfo)
		ss.Logger.Log(context.Background(), logLevel, msg, "plugin", pluginName)
	})

	return hostObj
}

func (ss *ScriptsService) responseObjectFactory(targetVM *goja.Runtime, url string) goja.Value {
	responseMap := map[string]any{
		"status": 200,
		"url":    url,
		"json": func() map[string]any {
			return map[string]any{"success": true}
		},
	}

	return targetVM.ToValue(responseMap)
}

func (ss *ScriptsService) RegisterScriptAndBindToBus(topic models.EventKey, scriptID string, rawJsString string) error {
	log := ss.Logger.With("scriptId", scriptID, "topic", topic)

	ss.mu.Lock()

	program, err := goja.Compile(scriptID, rawJsString, true)
	if err != nil {
		return fmt.Errorf("javascript compilation error: %w", err)
	}

	if val, exists := ss.cachedScripts[scriptID]; exists {
		val.Unsubscribe()
		delete(ss.cachedScripts, scriptID)
	}
	ss.mu.Unlock()

	re := regexp.MustCompile(`plugins\.(\w+)`)
	matches := re.FindAllStringSubmatch(rawJsString, -1)

	detectedPlugins := make([]string, 0, len(matches))
	seen := make(map[string]struct{})

	for _, match := range matches {
		if len(match) > 1 {
			pluginName := match[1]
			if _, exists := seen[pluginName]; !exists {
				seen[pluginName] = struct{}{}
				detectedPlugins = append(detectedPlugins, pluginName)
			}
		}
	}

	unsub := application.Get().Event.On(string(topic), func(customEvent *application.CustomEvent) {
		vm := goja.New()
		vm.SetFieldNameMapper(goja.TagFieldNameMapper("json", true))

		pluginsObj := vm.NewObject()
		_ = vm.Set("plugins", pluginsObj)

		for _, name := range detectedPlugins {
			ss.mu.RLock()
			plugin, exists := ss.cachedPlugins[name]
			ss.mu.RUnlock()
			if !exists {
				log.Error("tried to use a non-existant plugin", "plugin", name)
			}

			moduleObj := vm.NewObject()
			moduleExports := vm.NewObject()
			_ = moduleObj.Set("exports", moduleExports)
			_ = vm.Set("module", moduleObj)
			_ = vm.Set("exports", moduleExports)

			scopedHost := ss.CreateScopedHostObject(vm, plugin.Name, plugin.Permissions)
			_ = vm.Set("host", scopedHost)

			_, evalErr := vm.RunProgram(plugin.Program)
			if evalErr != nil {
				continue
			}

			rawExports := moduleObj.Get("exports").ToObject(vm)
			boundPluginInstance := vm.NewObject()

			for _, key := range rawExports.Keys() {
				exportedValue := rawExports.Get(key)
				if rawFunc, ok := goja.AssertFunction(exportedValue); ok {
					localHost := scopedHost

					boundMethod := vm.ToValue(func(call goja.FunctionCall) goja.Value {
						originalHost := vm.Get("host")
						_ = vm.Set("host", localHost)

						res, innerErr := rawFunc(goja.Undefined(), call.Arguments...)

						_ = vm.Set("host", originalHost)

						if innerErr != nil {
							panic(innerErr)
						}
						return res
					})
					_ = boundPluginInstance.Set(key, boundMethod)
				} else {
					_ = boundPluginInstance.Set(key, exportedValue)
				}
			}

			_ = pluginsObj.Set(name, boundPluginInstance)
		}

		userHost := vm.NewObject()
		_ = userHost.Set("log", func(level string, msg string) {
			logLevel := utils.ParseLogLevel(level, slog.LevelInfo)
			log.Log(context.Background(), logLevel, msg)
		})

		_ = vm.Set("host", userHost)
		_ = vm.Set("eventName", customEvent.Name)
		_ = vm.Set("eventData", customEvent.Data)
		_ = vm.Set("module", goja.Undefined())
		_ = vm.Set("exports", goja.Undefined())

		// timeBudget := time.Millisecond * 150
		// timer := time.AfterFunc(timeBudget, func() {
		// 	vm.Interrupt("Script execution exceeded allocated time budget limit")
		// })

		_, err = vm.RunProgram(program)
		// timer.Stop()

		if err != nil {
			log.Error("script runtime execution crashed", "error", err.Error())
		}
	})

	ss.cachedScripts[scriptID] = &CompiledScript{
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

func (ss *ScriptsService) GetMonacoEnvironment(topic models.EventKey) (string, error) {
	provider, exists := ss.typeRegistry[topic]

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

		innerFields = utils.GenerateTSFields(provider, string(topic))
	}

	jsPluginDeclarations, _ := ss.GetDynamicPluginDefinitions()

	fullEnvironment := fmt.Sprintf(`
%s

interface %s {
%s}


declare const eventName: string;
declare const eventData: %s;

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
	ss.RegisterScriptAndBindToBus(
		models.EventKeyManualInvoke,
		"test_script",
		`host.log("info", plugins.test_plugin.Hello())`,
	)
	ss.RegisterScriptAndBindToBus(
		models.EventKeyManualInvoke,
		"network_test_script",
		`host.log("info", JSON.stringify(plugins.test_plugin.sendToLogServer("test 1234")))`,
	)

	application.Get().Event.Emit(string(models.EventKeyManualInvoke))

	return nil
}
