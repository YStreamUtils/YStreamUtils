package models

type UserProfile struct {
	DisplayName string `json:"displayName"`
	AvatarURL   string `json:"avatarUrl"`
	ChannelID   string `json:"channelID"`
	Handle      string `json:"handle"`
}
