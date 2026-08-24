package services

import (
	"os"
	"path"
	"path/filepath"

	"github.com/BurntSushi/toml"
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
	BaseService
	settingsPath string
	Settings     *Settings
}

func NewSettingsService(settingsPath string) *SettingsService {
	s := &SettingsService{
		BaseService:  NewBaseService("SettingsService"),
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
	s.Logger.Info("trying to load settings configuration from file system", "path", s.settingsPath)

	_, err := toml.DecodeFile(s.settingsPath, s.Settings)
	if err != nil {
		s.Logger.Error("failed to decode configuration file path target", "error", err.Error())
		return err
	}

	return nil
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
