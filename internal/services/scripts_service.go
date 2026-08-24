package services

import (
	"context"
	"fmt"
	"log/slog"
	"path/filepath"
	"strings"
	"sync"

	"github.com/dop251/goja"
	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
)

type ScriptsService struct {
	BaseService
	ctx           context.Context
	pluginService *PluginService
	mu            sync.RWMutex
	cachedScripts map[string]*goja.Program
	hostDtsPath   string
}

func NewScriptsService(ctx context.Context, plugins *PluginService) *ScriptsService {
	return &ScriptsService{
		BaseService:   NewBaseService("ScriptsService"),
		ctx:           ctx,
		pluginService: plugins,
		cachedScripts: make(map[string]*goja.Program),
		hostDtsPath:   filepath.Join("frontend", "src", "lib", "types", "script-host.d.ts"),
	}
}

func (ss *ScriptsService) RegisterScriptAndBindToBus(topic string, scriptID string, rawJsString string) error {
	log := ss.Logger.With("scriptId", scriptID, "topic", topic)

	ss.mu.Lock()
	defer ss.mu.Unlock()

	program, err := goja.Compile(scriptID, rawJsString, true)
	if err != nil {
		log.Error("javascript compilation syntax validation error", "error", err.Error())
		return fmt.Errorf("javascript compilation syntax validation error: %w", err)
	}

	ss.cachedScripts[scriptID] = program

	application.Get().Event.On(topic, func(event *application.CustomEvent) {
		vm := goja.New()

		vm.Set("payload", event.Data)

		hostObj := vm.NewObject()
		vm.Set("host", hostObj)

		hostObj.Set("log", func(level string, msg string) {
			logLevel := utils.ParseLogLevel(level, slog.LevelInfo)

			log.Log(context.Background(), logLevel, msg)
		})

		vm.Set("_invokeWasmAction", func(call goja.FunctionCall) goja.Value {
			if len(call.Arguments) < 2 {
				panic(vm.NewTypeError("InvokeAction requires pluginNamespace and actionName"))
			}

			pluginNs := call.Arguments[0].String()
			actionName := call.Arguments[1].String()
			wasmJsArgs := call.Arguments[2:]

			// Convert JavaScript arguments into numeric register array slices
			wasmArgs := make([]uint64, len(wasmJsArgs))
			for i, arg := range wasmJsArgs {
				wasmArgs[i] = uint64(arg.ToInteger())
			}

			res, err := ss.pluginService.InvokeAction(pluginNs, actionName, wasmArgs)
			if err != nil {
				log.Error("wasm execution fault context inside script execution", "plugin", pluginNs, "action", actionName, "error", err.Error())
				panic(vm.NewTypeError("WebAssembly execution fault context: ", err.Error()))
			}

			if len(res) > 0 {
				return vm.ToValue(res[0])
			}
			return goja.Undefined()
		})

		bootstrap := ss.generateJavascriptPluginObjectType()
		if _, err := vm.RunString(bootstrap); err != nil {
			log.Error("failed to run plugin proxy bootstrap", "error", err.Error())
			return
		}

		if _, err = vm.RunProgram(program); err != nil {
			log.Error("script execution crashed runtime context", "error", err.Error())
		}
	})

	log.Info("successfully registered and bound script to wails event bus")
	return nil
}

func (ss *ScriptsService) generateJavascriptPluginObjectType() string {
	var sb strings.Builder
	sb.WriteString("const plugins = {};\n")

	ss.pluginService.mu.RLock()
	defer ss.pluginService.mu.RUnlock()

	for ns, compiledModule := range ss.pluginService.compiledModules {
		fmt.Fprintf(&sb, "plugins.%s = {\n", ns)

		for _, exp := range compiledModule.ExportedFunctions() {
			funcName := exp.Name()
			if strings.HasPrefix(funcName, "_") || funcName == "main" || funcName == "memory" {
				continue
			}
			fmt.Fprintf(&sb, "    %s: (...args) => _invokeWasmAction('%s', '%s', { Arguments: args.map(a => ({ ToInteger: () => a })) }),\n", funcName, ns, funcName)
		}
		sb.WriteString("};\n")
	}

	return sb.String()
}

func (ss *ScriptsService) GetDynamicPluginDefinitions() (string, error) {
	var sb strings.Builder

	sb.WriteString("/**\n * Automatically generated runtime plugin definitions.\n */\n\n")
	sb.WriteString("declare namespace plugins {\n")

	ss.pluginService.mu.RLock()
	defer ss.pluginService.mu.RUnlock()

	for ns, compiledModule := range ss.pluginService.compiledModules {
		fmt.Fprintf(&sb, "    namespace %s {\n", ns)

		for _, exp := range compiledModule.ExportedFunctions() {
			funcName := exp.Name()
			if strings.HasPrefix(funcName, "_") || funcName == "main" || funcName == "memory" {
				continue
			}

			paramCount := len(exp.ParamTypes())
			paramsStr := make([]string, paramCount)
			for i := range paramCount {
				paramsStr[i] = fmt.Sprintf("arg%d: number", i)
			}

			returnType := "void"
			if len(exp.ResultTypes()) > 0 {
				returnType = "number"
			}

			fmt.Fprintf(&sb, "        function %s(%s): %s;\n", funcName, strings.Join(paramsStr, ", "), returnType)
		}
		sb.WriteString("    }\n\n")
	}

	sb.WriteString("}\n")
	return sb.String(), nil
}

func (ss *ScriptsService) ServiceStartup(ctx context.Context, options application.ServiceOptions) error {
	return nil
}
