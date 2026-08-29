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
	"github.com/ystreamutils/YStreamUtils/internal/bridges"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
)

type EmptyStruct struct{}

type CompiledPlugin struct {
	Program     *goja.Program
	Permissions []types.Permission
}

type CompiledScript struct {
	Program     *goja.Program
	Unsubscribe func()
}

type ScriptsService struct {
	Logger         *slog.Logger
	ctx            context.Context
	pluginService  *PluginService
	youtubeService *YouTubeService
	vault          *TokenVault
	hostTypes      string

	mu            sync.RWMutex
	typeRegistry  map[models.EventKey]any
	cachedScripts map[string]*CompiledScript
	cachedPlugins map[string]*CompiledPlugin

	caches map[string]*bridges.CacheBridge
}

func NewScriptsService(ctx context.Context, plugins *PluginService, youtubeService *YouTubeService, vault *TokenVault, hostTypes string) *ScriptsService {
	return &ScriptsService{
		Logger:         utils.NewServiceLogger("ScriptsService"),
		ctx:            ctx,
		pluginService:  plugins,
		youtubeService: youtubeService,
		vault:          vault,
		hostTypes:      hostTypes,
		cachedScripts:  make(map[string]*CompiledScript),
		typeRegistry:   make(map[models.EventKey]any),
		caches:         make(map[string]*bridges.CacheBridge),
	}
}

func (ss *ScriptsService) InitializeVMPool() error {
	ss.mu.Lock()
	defer ss.mu.Unlock()

	activePlugins := ss.pluginService.GetActivePlugins()
	ss.cachedPlugins = make(map[string]*CompiledPlugin, len(activePlugins))

	for name, p := range activePlugins {
		prg, err := goja.Compile(name, p.JavaScriptCode, true)
		if err != nil {
			ss.Logger.Error("plugin failed to compile", "plugin", name)
			continue
		}

		perms := make([]types.Permission, 0, len(p.Manifest.Permissions))
		for _, perm := range p.Manifest.Permissions {
			perms = append(perms, perm)
		}

		ss.cachedPlugins[name] = &CompiledPlugin{
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
		bridge := bridges.NewFetchBridge()
		networkObj := vm.NewObject()
		_ = networkObj.Set("fetch", bridge.Fetch)
		_ = hostObj.Set("network", networkObj)
	}

	if permMap[models.PermissionYoutube] {
		youtubeObj := vm.NewObject()
		_ = youtubeObj.Set("sendChatMessage", func(liveChatID string, message string) error {
			ss.Logger.Info("Plugin sending chat message", "plugin", pluginName, "liveChatID", liveChatID)
			return ss.youtubeService.SendMessage(liveChatID, message)
		})
		_ = hostObj.Set("youtube", youtubeObj)
	}

	_ = hostObj.Set("log", func(level string, msg string) {
		logLevel := utils.ParseLogLevel(level, slog.LevelInfo)
		ss.Logger.Log(context.Background(), logLevel, msg, "plugin", pluginName)
	})

	return hostObj
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
				continue
			}

			scopedHost := ss.CreateScopedHostObject(vm, name, plugin.Permissions)
			_ = vm.Set("host", scopedHost)

			_, evalErr := vm.RunProgram(plugin.Program)
			if evalErr != nil {
				log.Error("Failed to evaluate plugin", "plugin", name, "err", evalErr)
				continue
			}

			boundPluginInstance := vm.Get(name)

			_ = pluginsObj.Set(name, boundPluginInstance)
		}

		userHost := vm.NewObject()
		_ = userHost.Set("log", func(level string, msg string) {
			logLevel := utils.ParseLogLevel(level, slog.LevelInfo)
			log.Log(context.Background(), logLevel, msg)
		})

		youtubeObj := vm.NewObject()
		_ = youtubeObj.Set("sendChatMessage", func(liveChatID string, message string) error {
			ss.Logger.Info("Plugin sending chat message", "script", scriptID, "liveChatID", liveChatID)
			return ss.youtubeService.SendMessage(liveChatID, message)
		})
		_ = userHost.Set("youtube", youtubeObj)

		cacheBridge, exists := ss.caches[scriptID]
		if !exists {
			cacheBridge = bridges.NewCacheBridge(scriptID)
			ss.caches[scriptID] = cacheBridge
		}
		cacheBridge.Register(vm, userHost)

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

	jsPluginDeclarations := ss.pluginService.GetDynamicPluginDefinitions()

	fullEnvironment := fmt.Sprintf(`
%s

interface %s {
%s}


declare const eventName: string;
declare const eventData: %s;

%s
`, jsPluginDeclarations, typeName, innerFields, typeName, ss.hostTypes)

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

	return nil
}
