package models

import "time"

type StreamEventEnvelope struct {
	Event     string     `json:"event"`
	Platform  Platform   `json:"platform"`
	Timestamp *time.Time `json:"timestamp,omitempty"`
	Data      any        `json:"data"`
}

func NewStreamEvent(event string, platform Platform, data any) StreamEventEnvelope {
	now := time.Now()
	return StreamEventEnvelope{
		Event:     event,
		Platform:  platform,
		Timestamp: &now,
		Data:      data,
	}
}

type BaseUserData struct {
	AuthorID    string `json:"authorId"`
	Author      string `json:"author"`
	AuthorColor string `json:"authorColor"`
	MessageID   string `json:"messageId"`
	Message     string `json:"message"`
}

type StreamChatMessage struct {
	BaseUserData
}

type StreamSuperchatMessage struct {
	BaseUserData
	Amount string `json:"amount"`
}

type StreamCheerMessage struct {
	BaseUserData
	Bits int64 `json:"bits"`
}
