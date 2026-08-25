package models

import "time"

type StreamEventEnvelope[T any] struct {
	Event     StreamEventName `json:"event"`
	Platform  Platform        `json:"platform"`
	Timestamp *time.Time      `json:"timestamp,omitempty"`
	Data      T             `json:"data"`
}

type StreamEventName string

func NewStreamEvent[T any](event StreamEventName, platform Platform, data T) StreamEventEnvelope[T] {
	now := time.Now()
	return StreamEventEnvelope[T]{
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

const StreamChatMessage StreamEventName = "chat"

type StreamChatMessageEvent struct {
	BaseUserData
}

const StreamSuperchatMessage StreamEventName = "chat"

type StreamSuperchatMessageEvent struct {
	BaseUserData
	Amount string `json:"amount"`
}

const StreamCheerMessage StreamEventName = "chat"

type StreamCheerMessageEvent struct {
	BaseUserData
	Bits int64 `json:"bits"`
}
