package models

type LogEvent struct {
	Timestamp string         `json:"timestamp"`
	Level     string         `json:"level"`
	Message   string         `json:"message"`
	Context   map[string]any `json:"context,omitempty"`
}