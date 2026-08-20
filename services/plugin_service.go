package services

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"sync"

	"github.com/tetratelabs/wazero"
	"github.com/tetratelabs/wazero/api"
	"github.com/ystreamutils/YStreamUtils/logger"
)

type PluginService struct {
	ctx             context.Context
	wasmRuntime     wazero.Runtime
	mu              sync.RWMutex
	compiledModules map[string]wazero.CompiledModule
	pluginsDir      string
}

func NewPluginService(ctx context.Context, pluginsDir string) *PluginService {
	r := wazero.NewRuntime(ctx)

	ps := &PluginService{
		ctx:             ctx,
		wasmRuntime:     r,
		compiledModules: make(map[string]wazero.CompiledModule),
		pluginsDir:      pluginsDir,
	}

	_, _ = r.NewHostModuleBuilder("ystreamutils:plugin/host-functions").
		NewFunctionBuilder().WithFunc(ps.LogToHost).Export("log").
		Instantiate(ctx)

	return ps
}

func (ps *PluginService) LogToHost(ctx context.Context, mod api.Module, level uint32, offset uint32, byteCount uint32) {
	pluginNamespace := mod.Name()

	bytes, read := mod.Memory().Read(offset, byteCount)
	if !read {
		return
	}

	enumLevels := []string{"info", "warn", "error"}
	logLabel := "unknown"
	if level < uint32(len(enumLevels)) {
		logLabel = enumLevels[level]
	}

	switch logLabel {
		case "info":
			logger.LogInfo(pluginNamespace, string(bytes))
		case "warn":
			logger.LogWarn(pluginNamespace, string(bytes))
		case "error":
			logger.LogError(pluginNamespace, string(bytes))
		default:
			logger.Log(logLabel, pluginNamespace, string(bytes))
	}
}

func (ps *PluginService) LoadPlugins() error {
	ps.mu.Lock()
	defer ps.mu.Unlock()

	_ = os.MkdirAll(ps.pluginsDir, 0755)
	for k := range ps.compiledModules {
		delete(ps.compiledModules, k)
	}
	
	return filepath.Walk(ps.pluginsDir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}
		if !info.IsDir() && strings.HasSuffix(info.Name(), ".wasm") {
			wasmBytes, err := os.ReadFile(path)
			if err != nil {
				return err
			}

			compiled, err := ps.wasmRuntime.CompileModule(ps.ctx, wasmBytes)
			if err != nil {
				return err
			}

			namespace := strings.TrimSuffix(info.Name(), ".wasm")
			ps.compiledModules[namespace] = compiled
		}
		return nil
	})
}

func (ps *PluginService) InvokeAction(namespace string, action string, args []uint64) ([]uint64, error) {
	ps.mu.RLock()
	compiled, exists := ps.compiledModules[namespace]
	ps.mu.RUnlock()

	if !exists {
		return nil, fmt.Errorf("plugin %s not compiled", namespace)
	}

	config := wazero.NewModuleConfig().WithName(namespace)

	mod, err := ps.wasmRuntime.InstantiateModule(ps.ctx, compiled, config)
	if err != nil {
		return nil, err
	}
	defer mod.Close(ps.ctx)

	f := mod.ExportedFunction(action)
	if f == nil {
		return nil, fmt.Errorf("action %s missing", action)
	}

	return f.Call(ps.ctx, args...)
}
