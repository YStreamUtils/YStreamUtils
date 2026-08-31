package models

import "time"

type ScriptRecord struct {
	ID         string   `gorm:"primaryKey"`
	EventKey   EventKey `gorm:"not null"`
	SourceCode string   `gorm:"type:text;not null"`
	UpdatedAt  time.Time
}

func (ScriptRecord) TableName() string {
	return "scripts"
}

type KVCacheRecord struct {
	Key   string `gorm:"primaryKey"`
	Value string `gorm:"type:text;not null"`
}

func (KVCacheRecord) TableName() string {
	return "kv_cache"
}
