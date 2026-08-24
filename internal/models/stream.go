package models

const (
	EventTypeChat          = "chat"
	EventTypeMetricUpdate  = "metrics"
	EventTypeStatusOffline = "offline"
)

type StreamMetrics struct {
	ConcurrentViewers int64 `json:"concurrentViewers"`
}

type StreamChatMessage struct {
	MessageID string `json:"messageId"`
	Author    string `json:"author"`
	AuthorID  string `json:"authorId"`
	Message   string `json:"message"`
	Timestamp int64  `json:"timestamp"`
}

type StreamEvent struct {
	Platform Platform           `json:"platform"`
	ChatID   string             `json:"chatId"`
	Type     string             `json:"type"`
	Message  *StreamChatMessage `json:"message,omitempty"`
	Metrics  *StreamMetrics     `json:"metrics,omitempty"`
}
