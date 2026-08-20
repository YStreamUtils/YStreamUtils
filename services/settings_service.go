package services

import (
	"fmt"
	"os"
	"path"
	"path/filepath"

	"github.com/BurntSushi/toml"
	"github.com/ystreamutils/YStreamUtils/logger"
)

type Settings struct {
	UISettings UISettings `toml:"ui"`
}

type UISettings struct {
	Theme            string `toml:"theme"`
	Color            string `toml:"color"`
	FullCloseSidebar bool   `toml:"fullyCloseSidebar"`
}

var DefaultSettings = Settings{
	UISettings: UISettings{
		Theme:            "dark",
		Color:            "#9900ff",
		FullCloseSidebar: false,
	},
}

type SettingsService struct {
	settingsPath string
	Settings     *Settings
}

func NewSettingsService(settingsPath string) *SettingsService {
	s := &SettingsService{
		settingsPath: path.Join(settingsPath, "settings.toml"),
		Settings:     &Settings{},
	}

	if err := s.LoadSettings(); err != nil {
		*s.Settings = DefaultSettings
		_ = s.SaveSettings(s.Settings)
	}

	return s
}

func (s *SettingsService) LoadSettings() error {
	logger.LogInfo("SettingsService", fmt.Sprintf("Trying to load settings from: %v", s.settingsPath))
	_, err := toml.DecodeFile(s.settingsPath, s.Settings)
	if err != nil {
		logger.LogError("SettingsService", err.Error())
	}
	return err
}

func (s *SettingsService) SaveSettings(settings *Settings) error {
	if err := os.MkdirAll(filepath.Dir(s.settingsPath), 0755); err != nil {
		return err
	}

	f, err := os.OpenFile(s.settingsPath, os.O_WRONLY|os.O_CREATE|os.O_TRUNC, 0644)
	if err != nil {
		return err
	}
	defer f.Close()

	if err := toml.NewEncoder(f).Encode(settings); err != nil {
		return err
	}

	s.Settings = settings
	return nil
}

func (s *SettingsService) GetSettings() *Settings {
	if s.Settings == nil {
		return &DefaultSettings
	}
	return s.Settings
}
