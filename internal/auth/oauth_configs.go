package auth

import (
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"golang.org/x/oauth2"
	"golang.org/x/oauth2/google"
	"google.golang.org/api/youtube/v3"
)

var OAuthConfigs = map[models.Platform]*oauth2.Config{
	models.PlatformYouTube: YoutubeOAuthConfig,
	models.PlatformTwitch:  TwitchOAuthConfig,
}

var YoutubeOAuthConfig = &oauth2.Config{
	ClientID: "889238060090-q7ahh2p1v7pgb11s3e0pn8t1r1mc0cft.apps.googleusercontent.com",
	// Not actually a secret, PKCE
	ClientSecret: "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",
	Endpoint:     google.Endpoint,
	Scopes: []string{
		youtube.YoutubeForceSslScope,
		youtube.YoutubeReadonlyScope,
	},
	RedirectURL: "http://localhost:11974/callback",
}

var TwitchOAuthConfig = &oauth2.Config{
	ClientID:     "YOUR_TWITCH_CLIENT_ID",
	ClientSecret: "YOUR_TWITCH_CLIENT_SECRET",
	Endpoint: oauth2.Endpoint{
		AuthURL:  "https://twitch.tv",
		TokenURL: "https://twitch.tv",
	},
	Scopes:      []string{"chat:read", "moderator:manage:banned_users"},
	RedirectURL: "http://localhost:11974/callback",
}
