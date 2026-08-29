package auth

import (
	"encoding/json"
	"fmt"
	"log/slog"

	"github.com/zalando/go-keyring"
)

type KeyringStore[T any] struct {
	serviceName string
	logger      *slog.Logger
}

func NewKeyringStore[T any](serviceName string, logger *slog.Logger) *KeyringStore[T] {
	return &KeyringStore[T]{
		serviceName: serviceName,
		logger:      logger,
	}
}

func (k *KeyringStore[T]) Store(key string, value T) error {
	data, err := json.Marshal(value)
	if err != nil {
		k.logger.Error("Failed to marshal generic payload", slog.String("key", key), slog.Any("error", err))
		return fmt.Errorf("marshal failure: %w", err)
	}

	if err := keyring.Set(k.serviceName, key, string(data)); err != nil {
		k.logger.Error("Failed to write data to hardware OS keyring", slog.String("key", key), slog.Any("error", err))
		return fmt.Errorf("keyring write failure: %w", err)
	}
	return nil
}

func (k *KeyringStore[T]) Get(key string) (T, error) {
	var value T
	secretString, err := keyring.Get(k.serviceName, key)
	if err != nil {
		k.logger.Debug("No matching keys discovered in system vault", slog.String("key", key))
		return value, err
	}

	if err := json.Unmarshal([]byte(secretString), &value); err != nil {
		k.logger.Error("Corrupted data sequence found inside keyring", slog.String("key", key), slog.Any("error", err))
		return value, fmt.Errorf("unmarshal failure: %w", err)
	}
	return value, nil
}

func (k *KeyringStore[T]) Delete(key string) error {
	return keyring.Delete(k.serviceName, key)
}
