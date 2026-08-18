package services

import "sync"

type Event[T any] struct {
	Type    string
	Payload T
}

type EventHandler[T any] func(event Event[T])

type EventBusService struct {
	mu          sync.RWMutex
	subscribers map[string][]EventHandler[map[string]any]
	ch          chan Event[map[string]any]
}

func NewEventBusService(bufferSize int) *EventBusService {
	bus := &EventBusService{
		subscribers: make(map[string][]EventHandler[map[string]any]),
		ch:          make(chan Event[map[string]any], bufferSize),
	}
	go bus.startDispatcher()
	return bus
}

func (b *EventBusService) Subscribe(eventType string, handler EventHandler[map[string]any]) {
	b.mu.Lock()
	defer b.mu.Unlock()
	b.subscribers[eventType] = append(b.subscribers[eventType], handler)
}

func (b *EventBusService) Publish(eventType string, payload map[string]any) {
	b.ch <- Event[map[string]any]{
		Type:    eventType,
		Payload: payload,
	}
}

func (b *EventBusService) startDispatcher() {
	for event := range b.ch {
		b.mu.RLock()
		
		handlers := b.subscribers[event.Type]
		
		if wildcards, ok := b.subscribers["*"]; ok {
			handlers = append(handlers, wildcards...)
		}
		
		b.mu.RUnlock()

		for _, handler := range handlers {
			go handler(event)
		}
	}
}