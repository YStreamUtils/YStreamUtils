package services

import (
	"context"
	"fmt"
	"log/slog"

	"github.com/ystreamutils/YStreamUtils/internal/auth"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
	"golang.org/x/oauth2"
)

const AppServiceName = "live.ysnt.ystreamutils"

type tokenSourceFunc func() (*oauth2.Token, error)

func (f tokenSourceFunc) Token() (*oauth2.Token, error) {
	return f()
}

type OAuthConfig struct {
	ClientID     string `json:"client_id"`
	ClientSecret string `json:"client_secret"`
}

type TokenVault struct {
	Logger      *slog.Logger
	tokenStore  *auth.KeyringStore[*oauth2.Token]
	configStore *auth.KeyringStore[OAuthConfig]
}

func NewTokenVault() *TokenVault {
	base := utils.NewServiceLogger("TokenVault")
	return &TokenVault{
		Logger:      base,
		tokenStore:  auth.NewKeyringStore[*oauth2.Token](AppServiceName+".session", base),
		configStore: auth.NewKeyringStore[OAuthConfig](AppServiceName+".oauth-config", base),
	}
}

// ---- Sessions ----

func (v *TokenVault) GetValidSession(platform models.Platform) (*oauth2.Token, error) {
	reuseSource, err := v.GetTokenSource(platform)
	if err != nil {
		return nil, err
	}

	token, err := v.getSession(platform)
	if err != nil {
		return nil, err
	}

	freshToken, err := reuseSource.Token()
	if err != nil {
		v.Logger.Error("Failed validating or refreshing OAuth credential state", slog.String("platform", string(platform)), slog.Any("error", err))
		return nil, fmt.Errorf("oauth evaluation failure: %w", err)
	}

	if freshToken.AccessToken != token.AccessToken {
		v.Logger.Debug("OAuth session expired. Token successfully refreshed. Persisting update to vault...", slog.String("platform", string(platform)))
		if err := v.StoreSession(platform, freshToken); err != nil {
			v.Logger.Error("Failed updating keyring with refreshed credential token parameters", slog.String("platform", string(platform)), slog.Any("error", err))
		}
	}

	return freshToken, nil
}

func (v *TokenVault) GetTokenSource(platform models.Platform) (oauth2.TokenSource, error) {
	config, err := v.GetConfig(platform)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve oauth configuration: %w", err)
	}

	token, err := v.getSession(platform)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve session token: %w", err)
	}

	baseSource := config.TokenSource(context.Background(), token)

	savingSource := oauth2.ReuseTokenSource(token, tokenSourceFunc(func() (*oauth2.Token, error) {
		freshToken, err := baseSource.Token()
		if err != nil {
			return nil, err
		}

		v.Logger.Debug("OAuth session expired. Token successfully refreshed via provider.",
			slog.String("platform", string(platform)),
			slog.Time("new_expiry", freshToken.Expiry),
		)

		if err := v.StoreSession(platform, freshToken); err != nil {
			v.Logger.Error("Failed updating vault with refreshed token parameters",
				slog.String("platform", string(platform)),
				slog.Any("error", err),
			)
		}
		return freshToken, nil
	}))

	return savingSource, nil
}

func (v *TokenVault) StoreSession(platform models.Platform, token *oauth2.Token) error {
	v.Logger.Debug("Securely saving account token parameters to system vault", slog.String("platform", string(platform)))
	return v.tokenStore.Store(string(platform), token)
}

func (v *TokenVault) getSession(platform models.Platform) (*oauth2.Token, error) {
	token, err := v.tokenStore.Get(string(platform))
	if err != nil {
		v.Logger.Warn("No credentials matching platform discovered in system vault", slog.String("platform", string(platform)))
		return nil, err
	}
	return token, nil
}

func (v *TokenVault) DeleteSession(platform models.Platform) error {
	v.Logger.Debug("Wiping credentials permanently from OS keyring", slog.String("platform", string(platform)))
	return v.tokenStore.Delete(string(platform))
}

// ---- Configs ----

func (v *TokenVault) StoreConfig(platform models.Platform, cfg OAuthConfig) error {
	v.Logger.Debug("Storing updated OAuth application credentials in config store",
		slog.String("platform", string(platform)),
		slog.String("client_id", cfg.ClientID),
	)
	return v.configStore.Store(string(platform), cfg)
}

func (v *TokenVault) GetConfig(platform models.Platform) (*oauth2.Config, error) {
	storedConfig, err := v.configStore.Get(string(platform))
	if err != nil {
		return nil, err
	}

	baseConfig := auth.OAuthConfigs[platform]
	if baseConfig == nil {
		return nil, fmt.Errorf("no base blueprint config found for platform: %s", platform)
	}

	config := *baseConfig
	config.ClientID = storedConfig.ClientID
	config.ClientSecret = storedConfig.ClientSecret

	return &config, nil
}
