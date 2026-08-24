package services

import (
	"context"
	"fmt"
	"log/slog"
	"os"
	"path"
	"path/filepath"
	"strings"
	"sync"

	"github.com/tetratelabs/wazero"
	"github.com/tetratelabs/wazero/api"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
)

type PluginService struct {
	BaseService
	ctx             context.Context
	wasmRuntime     wazero.Runtime
	mu              sync.RWMutex
	compiledModules map[string]wazero.CompiledModule
	pluginsDir      string
}

func NewPluginService(ctx context.Context, pluginsDir string) *PluginService {
	r := wazero.NewRuntime(ctx)

	ps := &PluginService{
		BaseService:     NewBaseService("PluginService"),
		ctx:             ctx,
		wasmRuntime:     r,
		compiledModules: make(map[string]wazero.CompiledModule),
		pluginsDir:      path.Join(pluginsDir, "plugins"),
	}

	if _, err := os.Stat(ps.pluginsDir); os.IsNotExist(err) {
		err := os.MkdirAll(ps.pluginsDir, 0755)
		if err != nil {
			ps.Logger.Error("failed to create plugin directory space", "path", ps.pluginsDir, "error", err.Error())
			os.Exit(1)
		}
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

	logLevel := utils.ParseLogLevelNumerical(level, slog.LevelInfo)

	ps.Logger.LogAttrs(ctx, logLevel, "plugin log",
		slog.String("plugin", pluginNamespace),
		slog.String("message", string(bytes)),
	)
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
	log := ps.Logger.With(
		"plugin", namespace,
		"action", action,
	)

	ps.mu.RLock()
	compiled, exists := ps.compiledModules[namespace]
	ps.mu.RUnlock()

	if !exists {
		err := fmt.Errorf("plugin %s not compiled", namespace)
		log.Error("failed to invoke action: module not compiled", "error", err.Error())
		return nil, err
	}

	config := wazero.NewModuleConfig().WithName(namespace)

	log.Debug("instantiating wasm module")
	mod, err := ps.wasmRuntime.InstantiateModule(ps.ctx, compiled, config)
	if err != nil {
		log.Error("failed to instantiate wasm module", "error", err.Error())
		return nil, err
	}
	defer mod.Close(ps.ctx)

	f := mod.ExportedFunction(action)
	if f == nil {
		err := fmt.Errorf("action %s missing", action)
		log.Error("failed to find exported function", "error", err.Error())
		return nil, err
	}

	log.Debug("executing exported function", "args_count", len(args))

	results, err := f.Call(ps.ctx, args...)
	if err != nil {
		log.Error("wasm runtime execution panicked or errored", "error", err.Error())
		return nil, err
	}

	log.Info("plugin action executed successfully", "results_count", len(results))
	return results, nil
}
