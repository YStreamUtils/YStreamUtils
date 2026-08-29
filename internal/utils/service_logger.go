package utils

import "log/slog"

func NewServiceLogger(serviceName string) *slog.Logger {
	return slog.Default().With("service", serviceName)
}
