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
	Name   string `json:"name"`
	Event  string `json:"eventKey"`
	Source string `json:"source"`
}

type ScriptLoader struct {
	Logger          *slog.Logger
	mu              sync.RWMutex
	scriptService   *ScriptsService
	scriptsLocation string
	manifest        []*Script
}

func NewScriptLoader(scriptService *ScriptsService, location string) *ScriptLoader {
	loader := &ScriptLoader{
		Logger:          utils.NewServiceLogger("ScriptsService"),
		scriptService:   scriptService,
		scriptsLocation: location,
		manifest:        []*Script{},
	}

	if err := os.MkdirAll(location, 0755); err != nil {
		loader.Logger.Error("Failed initializing scripts directory tree", "err", err)
	} else {
		_ = loader.loadManifest()
	}

	return loader
}

func (sl *ScriptLoader) SaveScript(scriptId string, source string, event string) error {
	sl.mu.Lock()
	defer sl.mu.Unlock()

	fileName := fmt.Sprintf("%s.js", filepath.Clean(scriptId))
	filePath := filepath.Join(sl.scriptsLocation, fileName)
	if err := os.WriteFile(filePath, []byte(source), 0644); err != nil {
		return fmt.Errorf("failed to save script source file to disk: %w", err)
	}

	var existingScript *Script
	for _, s := range sl.manifest {
		if s.Name == scriptId {
			existingScript = s
			break
		}
	}

	if existingScript != nil {
		existingScript.Event = event
		existingScript.Source = fileName
	} else {
		sl.manifest = append(sl.manifest, &Script{
			Name:   scriptId,
			Event:  event,
			Source: fileName,
		})
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

	var match *Script
	for _, s := range sl.manifest {
		if s.Name == scriptId {
			match = s
			break
		}
	}

	if match == nil {
		return nil, errors.New("script entry metadata row not found in current manifest mapping matrix")
	}

	filePath := filepath.Join(sl.scriptsLocation, match.Source)
	sourceBytes, err := os.ReadFile(filePath)
	if err != nil {
		return nil, fmt.Errorf("failed reading script source code string asset matching disk path: %w", err)
	}

	return &Script{
		Name:   match.Name,
		Event:  match.Event,
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

	for _, s := range sl.manifest {
		scriptText, err := sl.LoadScript(s.Name)
		if err != nil {
			sl.Logger.Error("Skipping startup script activation: source file missing or corrupt",
				slog.String("scriptId", s.Name),
				slog.Any("error", err),
			)
			continue
		}

		if err := sl.scriptService.RegisterScriptAndBindToBus(models.EventKey(scriptText.Event), scriptText.Name, scriptText.Source); err != nil {
			sl.Logger.Error("Failed to bind script to Wails event bus",
				slog.String("scriptId", s.Name),
				slog.Any("error", err),
			)
			continue
		}

		sl.Logger.Info("Successfully registered script and bound to event bus via startup",
			slog.String("scriptId", s.Name),
			slog.String("eventKey", s.Event),
		)
	}

	return nil
}
