package services

import (
	"context"
	"fmt"
	"sync"

	"github.com/ystreamutils/YStreamUtils/internal/ports"
)

type ChatService struct {
	BaseService
	mu            sync.RWMutex
	drivers       map[string]ports.StreamChatDriver
	activeCancels map[string]context.CancelFunc
}

func NewChatService() *ChatService {
	return &ChatService{
		BaseService:   NewBaseService("ChatService"),
		drivers:       make(map[string]ports.StreamChatDriver),
		activeCancels: make(map[string]context.CancelFunc),
	}
}

func (s *ChatService) RegisterDriver(platform string, d ports.StreamChatDriver) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.drivers[platform] = d
}

func (s *ChatService) StartChatStream(platform string, channelID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	sessionKey := fmt.Sprintf("%s-%s", platform, channelID)
	if _, running := s.activeCancels[sessionKey]; running {
		return nil
	}

	driver, exists := s.drivers[platform]
	if !exists {
		return fmt.Errorf("no chat driver registered for platform: %s", platform)
	}

	ctx, cancel := context.WithCancel(context.Background())
	s.activeCancels[sessionKey] = cancel

	if err := driver.ConnectChat(ctx, channelID); err != nil {
		cancel()
		return err
	}

	return nil
}

func (s *ChatService) StopChatStream(platform string, channelID string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	sessionKey := fmt.Sprintf("%s-%s", platform, channelID)
	if cancel, exists := s.activeCancels[sessionKey]; exists {
		cancel()
		delete(s.activeCancels, sessionKey)
	}
}
