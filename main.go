package main

import (
	"context"
	"embed"
	"log"
	"log/slog"
	"os"
	"path/filepath"

	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/services"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
)

//go:embed all:frontend/dist
var assets embed.FS

//go:embed host.d.ts
var hostTypes string

const AppName = "YStreamUtils"

var userConfigDir string

func init() {
	userPath, err := os.UserConfigDir()
	if err != nil {
		log.Fatal("failed to get user config directory:", err)
	}

	userConfigDir = filepath.Join(userPath, AppName)

	if _, err := os.Stat(userConfigDir); os.IsNotExist(err) {
		err := os.MkdirAll(userConfigDir, 0755)
		if err != nil {
			log.Fatal("failed to create app directory space:", userConfigDir, err)
		}
	}
}

func setupServices() []application.Service {
	ctx := context.Background()
	vaultService := services.NewTokenVault()
	youtubeService, err := services.NewYouTubeService(ctx, vaultService)
	if err != nil {
		log.Fatal("failed to initialize YouTube service:", err)
	}

	authService := services.NewAuthService(vaultService)
	authService.RegisterProfileDriver(models.PlatformYouTube, youtubeService)

	metricsService := services.NewMetricsService()
	metricsService.RegisterDriver("youtube", youtubeService)

	chatService := services.NewChatService()
	chatService.RegisterDriver("youtube", youtubeService)

	settingsService := services.NewSettingsService(userConfigDir)
	pluginService := services.NewPluginService(ctx, userConfigDir, settingsService)

	scriptsService := services.NewScriptsService(ctx, pluginService, youtubeService, vaultService, hostTypes)

	databaseService, err := services.NewDatabaseService(filepath.Join(userConfigDir, "database.db"))
	if err != nil {
		panic(err)
	}

	return []application.Service{
		application.NewService(vaultService),
		application.NewService(authService),
		application.NewService(youtubeService),
		application.NewService(metricsService),
		application.NewService(chatService),
		application.NewService(pluginService),
		application.NewService(scriptsService),
		application.NewService(settingsService),
		application.NewService(databaseService),
	}
}

func main() {
	app := application.New(application.Options{
		Name:        "YStreamUtils",
		Description: "A demo of using raw HTML & CSS",
		Services:    setupServices(),
		Assets: application.AssetOptions{
			Handler: application.AssetFileServerFS(assets),
		},
		Mac: application.MacOptions{
			ApplicationShouldTerminateAfterLastWindowClosed: true,
		},
	})
	app.Env.Info().OS
	wailsHandler := utils.NewLogHandler(os.Stdout, app.Event, userConfigDir)
	slog.SetDefault(slog.New(wailsHandler))

	window := app.Window.NewWithOptions(application.WebviewWindowOptions{
		Title:  "YStreamUtils",
		Width:  1000,
		Height: 618,
		Mac: application.MacWindow{
			InvisibleTitleBarHeight: 50,
			Backdrop:                application.MacBackdropTranslucent,
			TitleBar:                application.MacTitleBarHiddenInset,
		},
		Frameless: true,
		Windows: application.WindowsWindow{
			NonClientRegionSupport: true,
		},
		BackgroundColour:           application.NewRGB(255, 255, 255),
		URL:                        "/",
		DevToolsEnabled:            true,
		DefaultContextMenuDisabled: false,
	})

	SetupSystemTray(app, window)

	err := app.Run()
	if err != nil {
		log.Fatal(err)
	}
}

func SetupSystemTray(app *application.App, window application.Window) {
	tray := app.SystemTray.New()

	iconData, _ := os.ReadFile("icon.png")
	tray.SetIcon(iconData)
	tray.SetTooltip("YStreamUtils - Running")

	tray.OnClick(func() {
		if window.IsVisible() {
			window.Hide()
		} else {
			window.Show()
			window.Focus()
		}
	})

	trayMenu := app.NewMenu()

	showItem := trayMenu.Add("Show Window")
	showItem.OnClick(func(ctx *application.Context) {
		window.Show()
		window.Focus()
	})

	trayMenu.AddSeparator()

	trayMenu.Add("Quit").OnClick(func(ctx *application.Context) {
		app.Quit()
	})

	tray.SetMenu(trayMenu)
}
