package models

type EventKey string

const EventKeyStreamChatMessage EventKey = "stream:chat_message"
const EventKeyYoutubeSuperchat EventKey = "stream:youtube:superchat"

var EventKeyDisplayNames = map[EventKey]string{
	EventKeyStreamChatMessage: "Stream Chat Message (all)",
	EventKeyYoutubeSuperchat:  "YouTube SuperChat",
}
