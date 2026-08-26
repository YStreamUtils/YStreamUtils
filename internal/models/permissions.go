package models

import "github.com/YStreamUtils/YStreamUtils-Plugin-Registry/ci/types"

const (
	PermissionNetwork types.Permission = "network"

	PermissionYoutube types.Permission = "youtube"
	PermissionTwitch  types.Permission = "twitch"
	PermissionKick    types.Permission = "kick"
	PermissionDiscord types.Permission = "discord"

	PermissionStorageRead  types.Permission = "storage:read"
	PermissionStorageWrite types.Permission = "storage:write"

	PermissionFilesystemRead  types.Permission = "filesystem:read"
	PermissionFilesystemWrite types.Permission = "filesystem:write"
)
