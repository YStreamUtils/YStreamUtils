package services

import "log/slog"

type BaseService struct {
	Logger *slog.Logger
}

func NewBaseService(serviceName string) BaseService {
	return BaseService{
		Logger: slog.Default().With("service", serviceName),
	}
}