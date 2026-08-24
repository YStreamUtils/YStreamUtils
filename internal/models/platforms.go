package models

import "log/slog"

type Platform string

const (
	PlatformTwitch  Platform = "twitch"
	PlatformYouTube Platform = "youtube"
	PlatformKick    Platform = "kick"
)

func (p Platform) String() string {
	return string(p)
}

func (p Platform) LogValue() slog.Value {
	return slog.StringValue(p.String())
}
