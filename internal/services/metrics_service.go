package services

import (
	"context"
	"fmt"
	"log/slog"
	"sync"

	"github.com/ystreamutils/YStreamUtils/internal/ports"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
)

type MetricsService struct {
	Logger  *slog.Logger
	mu      sync.RWMutex
	drivers map[string]ports.StreamMetricsDriver
}

func NewMetricsService() *MetricsService {
	return &MetricsService{
		Logger:  utils.NewServiceLogger("MetricsService"),
		drivers: make(map[string]ports.StreamMetricsDriver),
	}
}

func (s *MetricsService) RegisterDriver(platform string, d ports.StreamMetricsDriver) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.drivers[platform] = d
}

func (s *MetricsService) FetchConcurrentViewers(platform string, channelID string) (int64, error) {
	s.mu.RLock()
	driver, exists := s.drivers[platform]
	s.mu.RUnlock()

	if !exists {
		return 0, fmt.Errorf("no stats driver registered for platform: %s", platform)
	}
	return driver.GetConcurrentViewers(context.Background(), channelID)
}

func (s *MetricsService) GetActiveBroadcastVideoIDs(platform string, includeUpcoming bool) ([]string, error) {
	s.mu.RLock()
	driver, exists := s.drivers[platform]
	s.mu.RUnlock()

	if !exists {
		return nil, nil
	}
	return driver.FindActiveBroadcastVideoIDs(context.Background(), includeUpcoming)
}
