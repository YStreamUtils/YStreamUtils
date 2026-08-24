package services

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"

	"github.com/ystreamutils/YStreamUtils/internal/auth"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/zalando/go-keyring"
	"golang.org/x/oauth2"
)

const AppServiceName = "live.ysnt.ystreamutils"

type TokenSourceFunc func() (*oauth2.Token, error)

func (f TokenSourceFunc) Token() (*oauth2.Token, error) {
	return f()
}

type TokenVault struct {
	BaseService
}

func NewTokenVault() *TokenVault {
	return &TokenVault{
		BaseService: NewBaseService("TokenVault"),
	}
}

func (v *TokenVault) GetValidSession(platform models.Platform) (*oauth2.Token, error) {
	token, err := v.GetSession(platform)
	if err != nil {
		return nil, err
	}

	config := auth.OAuthConfigs[platform]

	baseSource := config.TokenSource(context.Background(), token)
	reuseSource := oauth2.ReuseTokenSource(token, baseSource)

	freshToken, err := reuseSource.Token()
	if err != nil {
		v.Logger.Error("Failed validating or refreshing OAuth credential state", slog.String("platform", string(platform)), slog.Any("error", err))
		return nil, fmt.Errorf("oauth evaluation failure: %w", err)
	}

	if freshToken.AccessToken != token.AccessToken {
		v.Logger.Info("OAuth session string expired. Token successfully refreshed. Persisting update to vault...", slog.String("platform", string(platform)))
		if err := v.StoreSession(platform, freshToken); err != nil {
			v.Logger.Error("Failed updating keyring with refreshed credential token parameters", slog.String("platform", string(platform)), slog.Any("error", err))
		}
	}

	return freshToken, nil
}

func (v *TokenVault) StoreSession(platform models.Platform, token *oauth2.Token) error {
	data, err := json.Marshal(token)
	if err != nil {
		v.Logger.Error("Failed to marshal native oauth2 token", slog.String("platform", string(platform)), slog.Any("error", err))
		return err
	}

	err = keyring.Set(AppServiceName, string(platform), string(data))
	if err != nil {
		v.Logger.Error("Failed to write token to hardware OS keyring", slog.String("platform", string(platform)), slog.Any("error", err))
		return err
	}

	v.Logger.Info("Securely saved account token parameters to system vault", slog.String("platform", string(platform)))
	return nil
}

func (v *TokenVault) GetSession(platform models.Platform) (*oauth2.Token, error) {
	secretString, err := keyring.Get(AppServiceName, string(platform))
	if err != nil {
		v.Logger.Warn("No credentials matching platform discovered in system vault", slog.String("platform", string(platform)))
		return nil, err
	}

	var token oauth2.Token
	if err := json.Unmarshal([]byte(secretString), &token); err != nil {
		v.Logger.Error("Corrupted token mapping sequence found inside keyring", slog.String("platform", string(platform)))
		return nil, err
	}

	return &token, nil
}

func (v *TokenVault) DeleteSession(platform models.Platform) error {
	v.Logger.Info("Wiping credentials permanently from OS keyring", slog.String("platform", string(platform)))
	return keyring.Delete(AppServiceName, string(platform))
}
