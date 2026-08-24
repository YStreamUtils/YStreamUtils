package ports

import (
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"golang.org/x/oauth2"
)

type SecretVault interface {
	StoreSession(platform models.Platform, token *oauth2.Token) error
	GetSession(platform models.Platform) (*oauth2.Token, error)
	DeleteSession(platform models.Platform) error
	GetValidSession(platform models.Platform) (*oauth2.Token, error)
}
