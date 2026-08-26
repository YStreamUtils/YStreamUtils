package services

import (
	"archive/zip"
	"bytes"
	"context"
	"fmt"
	"io"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"github.com/BurntSushi/toml"
	"github.com/YStreamUtils/YStreamUtils-Plugin-Registry/ci/types"
	"github.com/evanw/esbuild/pkg/api"
	"github.com/fsnotify/fsnotify"
	"github.com/wailsapp/wails/v3/pkg/application"
)

type ActivePlugin struct {
	Name           string
	JavaScriptCode string
	TypeScriptDefs string
	Functions      []string
	Manifest       types.PluginManifest
}

type PluginService struct {
	BaseService
	ctx           context.Context
	settings      *SettingsService
	mu            sync.RWMutex
	pluginDir     string
	activePlugins []ActivePlugin
}

func NewPluginService(ctx context.Context, pluginDir string, settings *SettingsService) *PluginService {
	return &PluginService{
		BaseService:   NewBaseService("PluginService"),
		ctx:           ctx,
		settings:      settings,
		pluginDir:     filepath.Join(pluginDir, "plugins"),
		activePlugins: make([]ActivePlugin, 0),
	}
}

func (ps *PluginService) FetchAllRegistryPlugins() ([]types.PluginManifest, error) {
	registryURLs := ps.settings.GetSettings().PluginSettings.Repositories
	if len(registryURLs) == 0 {
		return nil, fmt.Errorf("no plugin registry repository endpoints are configured in settings")
	}

	var unifiedPlugins []types.PluginManifest
	seenPlugins := make(map[string]bool)

	for _, url := range registryURLs {
		if strings.TrimSpace(url) == "" {
			continue
		}

		ps.Logger.Info("Querying plugin registry workspace source", "url", url)
		plugins, err := ps.fetchSingleRegistry(url)
		if err != nil {
			ps.Logger.Error("Skipping failed registry destination pipeline context", "url", url, "error", err)
			continue
		}

		for _, plugin := range plugins {
			uniqueKey := strings.ToLower(plugin.Name)
			if !seenPlugins[uniqueKey] {
				seenPlugins[uniqueKey] = true
				unifiedPlugins = append(unifiedPlugins, plugin)
			}
		}
	}

	return unifiedPlugins, nil
}

func (ps *PluginService) fetchSingleRegistry(url string) ([]types.PluginManifest, error) {
	resp, err := http.Get(url)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("registry connection map responded with invalid status code: %s", resp.Status)
	}

	var distribution types.RegistryDistribution
	if _, err := toml.DecodeReader(resp.Body, &distribution); err != nil {
		return nil, err
	}

	return distribution.Plugins, nil
}

func (ps *PluginService) DownloadAndInstallPlugin(manifest types.PluginManifest) error {
	if manifest.Name == "" || manifest.EntryPoint == "" || manifest.Source.Owner == "" || manifest.Source.Repository == "" || manifest.Version == "" {
		return fmt.Errorf("cannot process manifest: missing explicit identification or version metadata properties")
	}

	if !strings.HasPrefix(manifest.Version, "v") {
		manifest.Version = "v" + manifest.Version
	}

	downloadURL := fmt.Sprintf(
		"https://github.com/%s/%s/releases/download/%s/%s.zip",
		manifest.Source.Owner,
		manifest.Source.Repository,
		manifest.Version,
		manifest.Name,
	)

	ps.Logger.Info("Streaming structured zip distribution target archive", "url", downloadURL)

	resp, err := http.Get(downloadURL)
	if err != nil {
		return fmt.Errorf("failed to fetch download package archive asset: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return fmt.Errorf("http bundle asset download failed status: %s", resp.Status)
	}

	targetDir := filepath.Join(ps.pluginDir, manifest.Name)
	if err := os.MkdirAll(targetDir, 0755); err != nil {
		return fmt.Errorf("failed to create target plugin directory path: %w", err)
	}

	bodyBytes, err := io.ReadAll(resp.Body)
	if err != nil {
		return fmt.Errorf("failed to read downloaded stream: %w", err)
	}

	zipReader, err := zip.NewReader(bytes.NewReader(bodyBytes), int64(len(bodyBytes)))
	if err != nil {
		return fmt.Errorf("failed to initialize zip extraction framework layer: %w", err)
	}

	for _, file := range zipReader.File {
		parts := strings.SplitN(file.Name, "/", 2)
		var relativePath string
		if len(parts) == 2 && parts[0] == manifest.Name {
			relativePath = parts[1]
		} else {
			relativePath = file.Name
		}

		if strings.ToLower(relativePath) == "manifest.toml" {
			continue
		}

		filePath := filepath.Join(targetDir, relativePath)
		if file.FileInfo().IsDir() {
			_ = os.MkdirAll(filePath, file.Mode())
			continue
		}

		if err := os.MkdirAll(filepath.Dir(filePath), 0755); err != nil {
			return err
		}

		dstFile, err := os.OpenFile(filePath, os.O_WRONLY|os.O_CREATE|os.O_TRUNC, file.Mode())
		if err != nil {
			return err
		}

		srcFile, err := file.Open()
		if err != nil {
			dstFile.Close()
			return err
		}

		_, err = io.Copy(dstFile, srcFile)
		srcFile.Close()
		dstFile.Close()
		if err != nil {
			return err
		}
	}

	var buffer bytes.Buffer
	encoder := toml.NewEncoder(&buffer)
	if err := encoder.Encode(manifest); err != nil {
		return fmt.Errorf("failed to encode registry manifest metadata layout reference: %w", err)
	}

	manifestPath := filepath.Join(targetDir, "manifest.toml")
	if err := os.WriteFile(manifestPath, buffer.Bytes(), 0644); err != nil {
		return fmt.Errorf("failed persisting local workspace copy manifest: %w", err)
	}

	return ps.ReloadLocalPlugins()
}

func (ps *PluginService) GetActivePlugins() []ActivePlugin {
	ps.mu.RLock()
	defer ps.mu.RUnlock()
	return ps.activePlugins
}

func (ps *PluginService) ReloadLocalPlugins() error {
	ps.mu.Lock()
	defer ps.mu.Unlock()

	if err := os.MkdirAll(ps.pluginDir, 0755); err != nil {
		return fmt.Errorf("unable setup plugin base directory framework: %w", err)
	}

	entries, err := os.ReadDir(ps.pluginDir)
	if err != nil {
		return fmt.Errorf("failed reading dynamic local disk configurations: %w", err)
	}

	newActivePlugins := make([]ActivePlugin, 0)

	for _, entry := range entries {
		if !entry.IsDir() {
			continue
		}

		ps.Logger.Info("searching directory for manifest", "directory", entry.Name())
		folderPath := filepath.Join(ps.pluginDir, entry.Name())
		manifestPath := filepath.Join(folderPath, "manifest.toml")

		if _, err := os.Stat(manifestPath); os.IsNotExist(err) {
			ps.Logger.Error("failed to locate manifest toml", "error", err)
			continue
		}

		var manifest types.PluginManifest
		if _, err := toml.DecodeFile(manifestPath, &manifest); err != nil {
			ps.Logger.Error("failed decoding manifest toml", "error", err)
			continue
		}
		ps.Logger.Info(fmt.Sprintf("Found plugin %s at %s", manifest.Name, manifestPath))

		fullEntryPointPath := filepath.Join(folderPath, manifest.EntryPoint)

		buildResult := api.Build(api.BuildOptions{
			EntryPoints: []string{fullEntryPointPath},
			Bundle:      true,
			Write:       false,
			Target:      api.ES5,
			Format:      api.FormatCommonJS,
			TreeShaking: api.TreeShakingFalse,
			LogLevel:    api.LogLevelSilent,
		})

		if len(buildResult.Errors) > 0 || len(buildResult.OutputFiles) == 0 {
			continue
		}

		bundledJSCode := string(buildResult.OutputFiles[0].Contents)

		typeDefs := ""
		defFilePath := filepath.Join(folderPath, "index.d.ts")
		if info, err := os.Stat(defFilePath); err == nil && !info.IsDir() {
			if content, err := os.ReadFile(defFilePath); err == nil {
				typeDefs = string(content)
			}
		}

		newActivePlugins = append(newActivePlugins, ActivePlugin{
			Name:           manifest.Name,
			JavaScriptCode: bundledJSCode,
			TypeScriptDefs: typeDefs,
			Functions:      []string{},
			Manifest:       manifest,
		})
	}

	ps.activePlugins = newActivePlugins
	return nil
}

func (ps *PluginService) StartDevPluginWatcher(onReload func()) {
	watcher, err := fsnotify.NewWatcher()
	if err != nil {
		ps.Logger.Error("Failed to initialize dev plugin watcher", "error", err)
		return
	}

	go func() {
		defer watcher.Close()

		var lastReload time.Time

		for {
			select {
			case event, ok := <-watcher.Events:
				if !ok {
					return
				}

				if event.Has(fsnotify.Write) {
					ext := filepath.Ext(event.Name)
					if ext == ".js" || ext == ".toml" || ext == ".ts" {
						if time.Since(lastReload) < 500*time.Millisecond {
							continue
						}
						lastReload = time.Now()

						ps.Logger.Info("dev plugin file modification caught! Auto-recompiling...", "file", filepath.Base(event.Name))

						_ = ps.ReloadLocalPlugins()

						onReload()
					}
				}
			case err, ok := <-watcher.Errors:
				if !ok {
					return
				}
				ps.Logger.Error("Watcher error encountered", "error", err)
			}
		}
	}()

	entries, err := os.ReadDir(ps.pluginDir)
	if err != nil {
		return
	}

	for _, entry := range entries {
		if entry.IsDir() && strings.HasPrefix(entry.Name(), "_") {
			devFolderPath := filepath.Join(ps.pluginDir, entry.Name())
			_ = watcher.Add(devFolderPath)
			ps.Logger.Info("watching development plugin for auto-reload on save", "path", devFolderPath)
		}
	}
}

func (ps *PluginService) ServiceStartup(ctx context.Context, options application.ServiceOptions) error {
	return ps.ReloadLocalPlugins()
}
