package services

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"log/slog"
	"os"
	"path/filepath"
	"sync"

	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
)

type Script struct {
	Event  models.EventKey `json:"eventKey"`
	Source string          `json:"source"` // Contains the filename (e.g., "abc.js")
}

type ScriptLoader struct {
	Logger          *slog.Logger
	mu              sync.RWMutex
	scriptService   *ScriptsService
	scriptsLocation string
	manifest        map[string]*Script
}

func NewScriptLoader(scriptService *ScriptsService, location string) *ScriptLoader {
	scriptPath := filepath.Join(location, "scripts")
	loader := &ScriptLoader{
		Logger:          utils.NewServiceLogger("ScriptsService"),
		scriptService:   scriptService,
		scriptsLocation: scriptPath,
		manifest:        make(map[string]*Script),
	}

	if err := os.MkdirAll(loader.scriptsLocation, 0755); err != nil {
		loader.Logger.Error("Failed initializing scripts directory tree", "err", err)
	} else {
		_ = loader.loadManifest()
	}

	return loader
}

func (sl *ScriptLoader) SaveScript(scriptId string, source string, event models.EventKey) error {
	sl.mu.Lock()
	defer sl.mu.Unlock()

	fileName := fmt.Sprintf("%s.js", filepath.Clean(scriptId))
	filePath := filepath.Join(sl.scriptsLocation, fileName)
	if err := os.WriteFile(filePath, []byte(source), 0644); err != nil {
		return fmt.Errorf("failed to save script source file to disk: %w", err)
	}

	script, exists := sl.manifest[scriptId]
	if exists {
		script.Event = event
		script.Source = fileName
	} else {
		sl.manifest[scriptId] = &Script{
			Event:  event,
			Source: fileName,
		}
	}

	if err := sl.saveManifest(); err != nil {
		return fmt.Errorf("failed to commit manifest index update metadata pass: %w", err)
	}

	sl.Logger.Info("Script successfully saved and manifest indexed", "scriptId", scriptId, "eventKey", event)
	return nil
}

func (sl *ScriptLoader) LoadScript(scriptId string) (*Script, error) {
	sl.mu.RLock()
	defer sl.mu.RUnlock()
	return sl.loadScriptUnlocked(scriptId)
}

func (sl *ScriptLoader) loadScriptUnlocked(scriptId string) (*Script, error) {
	script, exists := sl.manifest[scriptId]
	if !exists {
		return nil, errors.New("script entry metadata row not found in current manifest mapping matrix")
	}

	filePath := filepath.Join(sl.scriptsLocation, script.Source)
	sourceBytes, err := os.ReadFile(filePath)
	if err != nil {
		return nil, fmt.Errorf("failed reading script source code string asset matching disk path: %w", err)
	}

	return &Script{
		Event:  script.Event,
		Source: string(sourceBytes),
	}, nil
}

func (sl *ScriptLoader) loadManifest() error {
	manifestPath := filepath.Join(sl.scriptsLocation, "manifest.json")

	if _, err := os.Stat(manifestPath); os.IsNotExist(err) {
		return nil
	}

	data, err := os.ReadFile(manifestPath)
	if err != nil {
		return err
	}

	return json.Unmarshal(data, &sl.manifest)
}

func (sl *ScriptLoader) saveManifest() error {
	manifestPath := filepath.Join(sl.scriptsLocation, "manifest.json")

	jsonBytes, err := json.MarshalIndent(sl.manifest, "", "  ")
	if err != nil {
		return err
	}

	return os.WriteFile(manifestPath, jsonBytes, 0644)
}

func (sl *ScriptLoader) ServiceStartup(ctx context.Context, options application.ServiceOptions) error {
	sl.mu.RLock()
	defer sl.mu.RUnlock()

	sl.Logger.Info("Initializing ScriptLoader startup lifecycle", "total_scripts", len(sl.manifest))

	for name, s := range sl.manifest {
		scriptText, err := sl.loadScriptUnlocked(name)
		if err != nil {
			sl.Logger.Error("Skipping startup script activation: source file missing or corrupt",
				slog.String("scriptId", name),
				slog.Any("error", err),
			)
			continue
		}

		if err := sl.scriptService.RegisterScriptAndBindToBus(models.EventKey(scriptText.Event), name, scriptText.Source); err != nil {
			sl.Logger.Error("Failed to bind script to Wails event bus",
				slog.String("scriptId", name),
				slog.Any("error", err),
			)
			continue
		}

		sl.Logger.Info("Successfully registered script and bound to event bus via startup",
			slog.String("scriptId", name),
			slog.String("eventKey", string(s.Event)),
		)
	}

	return nil
}

func (sl *ScriptLoader) GetManifest() map[string]*Script {
	sl.mu.RLock()
	defer sl.mu.RUnlock()

	copiedManifest := make(map[string]*Script, len(sl.manifest))
	for k, v := range sl.manifest {
		filePath := filepath.Join(sl.scriptsLocation, v.Source)
		sourceBytes, err := os.ReadFile(filePath)

		var sourceContent string
		if err != nil {
			sl.Logger.Error("Failed reading script source asset for manifest transfer", "scriptId", k, "err", err)
			sourceContent = fmt.Sprintf("// Error loading script source file: %v", err)
		} else {
			sourceContent = string(sourceBytes)
		}

		copiedManifest[k] = &Script{
			Event:  v.Event,
			Source: sourceContent,
		}
	}
	return copiedManifest
}
