package models

type Permission string

const (
	PermissionNetwork Permission = "network"

	PermissionYoutube Permission = "youtube"
	PermissionTwitch  Permission = "twitch"
	PermissionKick    Permission = "kick"
	PermissionDiscord Permission = "discord"

	PermissionStorageRead  Permission = "storage:read"
	PermissionStorageWrite Permission = "storage:write"

	PermissionFilesystemRead  Permission = "filesystem:read"
	PermissionFilesystemWrite Permission = "filesystem:write"
)
