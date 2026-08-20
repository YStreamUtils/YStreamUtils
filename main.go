package main

import (
	"context"
	"embed"
	"fmt"
	"os"
	"path"

	"log"

	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/logger"
	"github.com/ystreamutils/YStreamUtils/services"
)

//go:embed all:frontend/dist
var assets embed.FS

const AppName = "YStreamUtils"

func init() {
	userPath, err := os.UserConfigDir()
	if err != nil {
		logger.LogError("Init", fmt.Sprintf("Failed to get user config directory: %v", err.Error()))
		os.Exit(1)
	}

	configPath := path.Join(userPath, AppName)

	if _, err := os.Stat(configPath); os.IsNotExist(err) {
		err := os.MkdirAll(configPath, 0755)
		if err != nil {
			logger.LogError("INIT", fmt.Sprintf("Failed to create config directory: %v", err.Error()))
			os.Exit(1)
		}
	}

	logger.LogInfo("Init", fmt.Sprintf("Config Directory is: %v", configPath))
	os.Setenv("YSU_USER_CONFIG_DIR", configPath)
	logger.LogInfo("INIT", os.Getenv("YSU_USER_CONFIG_DIR"))
}

func setupServices() []application.Service {
	ctx := context.Background()

	eventBusService := services.NewEventBusService(100)
	pluginService := services.NewPluginService(ctx, "./plugins")

	err := pluginService.LoadPlugins()
	if err != nil {
		fmt.Printf("[Wasm Runtime Fault] Failed to complete baseline plugin compilation initialization: %v\n", err)
	}

	scriptsService := services.NewScriptsService(ctx, eventBusService, pluginService)

	_ = scriptsService.RegisterScriptAndBindToBus("TwitchFollow", "on_welcome_follow", `
		host.log("info", "Channel macro triggered over Go generic channel!");
	`)

	settingsService := services.NewSettingsService(os.Getenv("YSU_USER_CONFIG_DIR"))

	return []application.Service{
		application.NewService(eventBusService),
		application.NewService(pluginService),
		application.NewService(scriptsService),
		application.NewService(settingsService),
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
		BackgroundColour: application.NewRGB(255, 255, 255),
		URL:              "/",
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
