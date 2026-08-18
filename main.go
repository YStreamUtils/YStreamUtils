package main

import (
	"context"
	"embed"
	"fmt"
	"os"

	"log"

	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/services"
)

//go:embed all:frontend/dist
var assets embed.FS

func main() {
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

	app := application.New(application.Options{
		Name:        "YStreamUtils",
		Description: "A demo of using raw HTML & CSS",
		Services: []application.Service{
			application.NewService(eventBusService),
			application.NewService(pluginService),
			application.NewService(scriptsService),
		},
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

	eventBusService.Publish("TwitchFollow", map[string]interface{}{
		"username": "WhaleStreamer99",
	})

	err = app.Run()

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
