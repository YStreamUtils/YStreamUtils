package utils

import (
	"context"
	"fmt"
	"io"
	"log/slog"
	"os"
	"path/filepath"
	"sync"

	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/internal/models"
)

const (
	colorReset  = "\033[0m"
	colorDebug  = "\033[35m" // Magenta
	colorInfo   = "\033[32m" // Green
	colorWarn   = "\033[33m" // Yellow
	colorError  = "\033[31m" // Red
	colorAttr   = "\033[90m" // Dark Gray
	colorWhite  = "\033[37m" // Standard White
	colorWhiteB = "\033[97m" // High-Intensity/Bright White
)

type LogHandler struct {
	mu       *sync.Mutex
	out      io.Writer
	fileOut  io.Writer
	attrs    []slog.Attr
	groups   []string
	eventBus *application.EventManager
}

func NewLogHandler(out io.Writer, eventBus *application.EventManager, logLocation string) *LogHandler {
	if out == nil {
		out = os.Stdout
	}

	var fileOut io.Writer

	logPath := filepath.Join(logLocation, "latest.log")

	if f, err := os.OpenFile(logPath, os.O_CREATE|os.O_WRONLY|os.O_TRUNC, 0644); err == nil {
		fileOut = f
	} else {
		fmt.Fprintf(out, "Failed to create log file at %s: %v\n", logPath, err)
	}

	return &LogHandler{
		mu:       &sync.Mutex{},
		out:      out,
		fileOut:  fileOut,
		eventBus: eventBus,
	}
}

func (h *LogHandler) Enabled(_ context.Context, _ slog.Level) bool {
	return true
}

func (h *LogHandler) Handle(_ context.Context, r slog.Record) error {
	h.mu.Lock()
	defer h.mu.Unlock()

	timeStr := r.Time.Format("Jan 02 15:04:05.000")

	var levelStr string
	var levelColor string

	switch r.Level {
	case slog.LevelDebug:
		levelStr = "DBG"
		levelColor = colorDebug
	case slog.LevelInfo:
		levelStr = "INF"
		levelColor = colorInfo
	case slog.LevelWarn:
		levelStr = "WRN"
		levelColor = colorWarn
	case slog.LevelError:
		levelStr = "ERR"
		levelColor = colorError
	default:
		levelStr = "LOG"
		levelColor = colorReset
	}

	fmt.Fprintf(h.out, "%s%s%s %s%s%s %s%s%s",
		colorAttr, timeStr, colorReset,
		levelColor, levelStr, colorReset,
		colorWhiteB, r.Message, colorReset)

	if h.fileOut != nil {
		fmt.Fprintf(h.fileOut, "%s %s %s", timeStr, levelStr, r.Message)
	}

	ctxMap := make(map[string]any)

	r.Attrs(func(a slog.Attr) bool {
		fmt.Fprintf(h.out, " %s%s=%v%s", colorAttr, a.Key, a.Value.Any(), colorReset)

		if h.fileOut != nil {
			fmt.Fprintf(h.fileOut, " %s=%v", a.Key, a.Value.Any())
		}

		ctxMap[a.Key] = a.Value.Any()
		return true
	})

	fmt.Fprintln(h.out)
	if h.fileOut != nil {
		fmt.Fprintln(h.fileOut)
	}

	if h.eventBus != nil {
		eventPayload := models.LogEvent{
			Timestamp: timeStr,
			Level:     levelStr,
			Message:   r.Message,
			Context:   ctxMap,
		}

		go h.eventBus.Emit("app:log", eventPayload)
	}

	return nil
}

func (h *LogHandler) WithAttrs(attrs []slog.Attr) slog.Handler {
	return &LogHandler{mu: h.mu, out: h.out, fileOut: h.fileOut, attrs: append(h.attrs, attrs...), groups: h.groups, eventBus: h.eventBus}
}

func (h *LogHandler) WithGroup(name string) slog.Handler {
	return &LogHandler{mu: h.mu, out: h.out, fileOut: h.fileOut, attrs: h.attrs, groups: append(h.groups, name), eventBus: h.eventBus}
}
