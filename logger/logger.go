package logger

import (
	"fmt"
	"strings"
	"time"
)

const (
	colorReset = "\033[0m"
	colorInfo  = "\033[34m"
	colorWarn  = "\033[33m"
	colorError = "\033[31m"
)

func Log(level string, ns string, msg string) {
	var colorCode string
	switch strings.ToLower(level) {
	case "info":
		colorCode = colorInfo
	case "warn":
		colorCode = colorWarn
	case "error":
		colorCode = colorError
	}

	logLabel := strings.ToUpper(level)
	formattedTime := time.Now().Format("01-02-2006 15:04:05")

	fmt.Printf("%s[%s][%s][%s]: %s%s\n",
		colorCode, formattedTime, logLabel, ns, msg, colorReset)
}

func LogInfo(ns string, msg string) {
	Log("info", ns, msg)
}

func LogWarn(ns string, msg string) {
	Log("warn", ns, msg)
}

func LogError(ns string, msg string) {
	Log("error", ns, msg)
}
