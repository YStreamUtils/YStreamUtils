package ports

import (
	"context"

	"github.com/ystreamutils/YStreamUtils/internal/models"
)

type StreamChatDriver interface {
	ConnectChat(ctx context.Context, channelID string) error
}

type StreamMetricsDriver interface {
	GetConcurrentViewers(ctx context.Context, channelID string) (int64, error)
	GetProfile(ctx context.Context) (*models.UserProfile, error)
	FindActiveBroadcastVideoIDs(ctx context.Context, includeUpcoming bool) ([]string, error)
}

type StreamProfileDriver interface {
	GetProfile(ctx context.Context) (*models.UserProfile, error)
	CreateClient(ctx context.Context) error
}
