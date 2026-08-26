package models

type SourceConfig struct {
	Repository  string `toml:"repository"`
	DownloadURL string `toml:"download_url"`
	EntryPoint  string `toml:"entry_point"`
}

type DocumentationConfig struct {
	Description string `toml:"description"`
}

type PluginManifest struct {
	Name          string              `toml:"name"`
	Version       string              `toml:"version"`
	Permissions   []Permission        `toml:"permissions"`
	Description   string              `toml:"description"`
	Authors       []string            `toml:"authors"`
	Source        SourceConfig        `toml:"source"`
	Documentation DocumentationConfig `toml:"documentation"`
}
