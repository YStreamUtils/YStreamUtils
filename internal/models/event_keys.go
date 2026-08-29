package models

type EventKey string

const EventKeyManualInvoke EventKey = "app:manual"

const EventKeyStreamChatMessage EventKey = "stream:chat_message"
const EventKeyYoutubeSuperchat EventKey = "stream:youtube:superchat"

const EventKeyApplicationLog EventKey = "app:log"

var EventKeyDisplayNames = map[EventKey]string{
	EventKeyManualInvoke:      "Manual Invoke",
	EventKeyStreamChatMessage: "Stream Chat Message (all)",
	EventKeyYoutubeSuperchat:  "YouTube SuperChat",
	EventKeyApplicationLog: "Application Logs",
}


