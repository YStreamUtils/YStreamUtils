package models

import "log/slog"

type Platform string

const (
	Youtube Platform = "youtube"
	Twitch  Platform = "twitch"
)

func (p Platform) String() string {
	return string(p)
}

func (p Platform) LogValue() slog.Value {
	return slog.StringValue(p.String())
}
