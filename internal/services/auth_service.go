package services

import (
	"context"
	"crypto/rand"
	"encoding/hex"
	"errors"
	"fmt"
	"log/slog"
	"net"
	"net/http"
	"time"

	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/internal/auth"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/ports"
	"golang.org/x/oauth2"
)

type AuthService struct {
	BaseService
	vault          ports.SecretVault
	profileDrivers map[models.Platform]ports.StreamProfileDriver
	Profiles       map[string]*models.UserProfile
}

func NewAuthService(v ports.SecretVault) *AuthService {
	return &AuthService{
		BaseService:    NewBaseService("AuthService"),
		vault:          v,
		profileDrivers: make(map[models.Platform]ports.StreamProfileDriver),
		Profiles:       make(map[string]*models.UserProfile),
	}
}

func (s *AuthService) RegisterProfileDriver(platform models.Platform, provider ports.StreamProfileDriver) {
	s.profileDrivers[platform] = provider
}

func (s *AuthService) LoginPlatform(ctx context.Context, platform models.Platform) (bool, error) {
	s.Logger.Info("Triggering interactive web browser authentication flow", slog.String("platform", string(platform)))

	baseConfig, exists := auth.OAuthConfigs[platform]
	if !exists {
		return false, fmt.Errorf("platform configuration profile unrecognized: %s", platform)
	}

	config := *baseConfig

	b := make([]byte, 16)
	if _, err := rand.Read(b); err != nil {
		return false, fmt.Errorf("failed to generate secure state token: %w", err)
	}
	stateToken := hex.EncodeToString(b)
	pkceVerifier := oauth2.GenerateVerifier()

	authCtx, authCancel := context.WithTimeout(ctx, 5*time.Minute)
	defer authCancel()

	codeChan := make(chan string, 1)

	mux := http.NewServeMux()
	mux.HandleFunc("/callback", func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Query().Get("state") != stateToken {
			http.Error(w, "Security validation failed", http.StatusBadRequest)
			authCancel()
			return
		}

		code := r.URL.Query().Get("code")
		if code == "" {
			http.Error(w, "Code parameter missing", http.StatusBadRequest)
			authCancel()
			return
		}

		w.Header().Set("Content-Type", "text/html; charset=utf-8")
		fmt.Fprintf(w, `
            <body style="font-family:sans-serif;background:#121214;color:#fff;text-align:center;padding:40px;">
                <h2 style="color:#4caf50;">✓ Link Approved!</h2>
                <p>You successfully authorized %s.</p>
                <p style="color:#888;font-size:13px;">You can safely close this web browser tab now.</p>
            </body>
        `, platform)

		codeChan <- code
	})

	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		return false, fmt.Errorf("failed to allocate socket: %w", err)
	}
	defer listener.Close()

	_, port, err := net.SplitHostPort(listener.Addr().String())
	if err != nil {
		return false, fmt.Errorf("failed to parse listener address: %w", err)
	}
	config.RedirectURL = fmt.Sprintf("http://127.0.0.1:%s/callback", port)

	authURL := config.AuthCodeURL(
		stateToken,
		oauth2.AccessTypeOffline,
		oauth2.ApprovalForce,
		oauth2.S256ChallengeOption(pkceVerifier),
	)

	server := &http.Server{Handler: mux}
	go func() {
		if err := server.Serve(listener); err != http.ErrServerClosed {
			authCancel()
		}
	}()

	go application.Get().Browser.OpenURL(authURL)

	cleanupServer := func() {
		shutdownCtx, shutdownCancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer shutdownCancel()
		_ = server.Shutdown(shutdownCtx)
	}

	select {
	case code := <-codeChan:
		cleanupServer()

		token, err := config.Exchange(authCtx, code, oauth2.VerifierOption(pkceVerifier))
		if err != nil {
			return false, fmt.Errorf("token exchange broken: %w", err)
		}

		if err = s.vault.StoreSession(platform, token); err != nil {
			return false, err
		}

		driver, exists := s.profileDrivers[platform]
		if !exists {
			return false, fmt.Errorf("no profile driver registered for platform: %s", platform)
		}

		if err = driver.CreateClient(ctx); err != nil {
			return false, err
		}

		s.Logger.Info("Login operation succeeded and saved to keyring", slog.String("platform", string(platform)))
		return true, nil

	case <-authCtx.Done():
		cleanupServer()

		if errors.Is(authCtx.Err(), context.Canceled) {
			return false, fmt.Errorf("authorization was cancelled by the user or application")
		}
		return false, fmt.Errorf("authorization timeout limit crossed")
	}
}

func (s *AuthService) GetProfile(platform models.Platform) (*models.UserProfile, error) {
	if profile, exists := s.Profiles[string(platform)]; exists && profile != nil {
		return profile, nil
	}

	driver, exists := s.profileDrivers[platform]
	if !exists {
		return nil, fmt.Errorf("no profile driver registered for platform: %s", platform)
	}

	ctx := context.Background()
	profile, err := driver.GetProfile(ctx)
	if err != nil {
		s.Logger.Error("Failed to fetch profile", slog.String("platform", string(platform)), slog.Any("error", err))
		return nil, err
	}

	s.Profiles[string(platform)] = profile
	s.Logger.Info("Fetched and cached profile", slog.String("platform", string(platform)))

	return profile, nil
}

func (s *AuthService) ServiceStartup(ctx context.Context, options application.Options) (bool, error) {
	for platform, baseConfig := range auth.OAuthConfigs {
		savedToken, err := s.vault.GetSession(platform)
		if err != nil {
			s.Logger.Warn("No saved session found for platform", slog.String("platform", string(platform)))
			continue
		}

		config := *baseConfig

		tokenSource := config.TokenSource(ctx, savedToken)

		currentToken, err := tokenSource.Token()
		if err != nil {
			_ = s.vault.DeleteSession(platform)
			s.Logger.Error("Session expired or revoked", slog.String("platform", string(platform)), slog.Any("error", err))
			continue
		}

		if currentToken.AccessToken != savedToken.AccessToken {
			if err := s.vault.StoreSession(platform, currentToken); err != nil {
				s.Logger.Error("Failed to persist refreshed token", slog.String("platform", string(platform)), slog.Any("error", err))
				continue
			}
			s.Logger.Info("Tokens updated and synchronized automatically during launch", slog.String("platform", string(platform)))
		}

		s.Logger.Info("User session authenticated successfully on launch", slog.String("platform", string(platform)))

		profile, err := s.GetProfile(platform)
		if err != nil {
			s.Logger.Error("Failed to get profile during startup", slog.String("platform", string(platform)), slog.Any("error", err))
			continue
		}

		if profile != nil {
			s.Profiles[string(platform)] = profile
		}
	}

	return true, nil
}
