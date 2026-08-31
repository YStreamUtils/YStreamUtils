package services

import (
	"context"
	"fmt"
	"log/slog"
	"time"

	"github.com/libtnb/sqlite"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
	"gorm.io/gorm"
	"gorm.io/gorm/clause"
)

type DatabaseService struct {
	logger *slog.Logger
	db     *gorm.DB
}

func NewDatabaseService(dbPath string) (*DatabaseService, error) {
	dsn := fmt.Sprintf("%s?_pragma=journal_mode(WAL)&_pragma=synchronous(NORMAL)", dbPath)
	db, err := gorm.Open(sqlite.Open(dsn), &gorm.Config{})
	if err != nil {
		return nil, fmt.Errorf("failed to open database connection: %w", err)
	}

	err = db.AutoMigrate(&models.ScriptRecord{}, &models.KVCacheRecord{})
	if err != nil {
		return nil, fmt.Errorf("failed running gorm database schema migrations: %w", err)
	}

	return &DatabaseService{logger: utils.NewServiceLogger("DatabaseService"), db: db}, nil
}

func (s *DatabaseService) SaveScript(id string, source string, event models.EventKey) error {
	record := models.ScriptRecord{
		ID:         id,
		EventKey:   event,
		SourceCode: source,
		UpdatedAt:  time.Now(),
	}

	return gorm.G[models.ScriptRecord](s.db, clause.OnConflict{UpdateAll: true}).Create(
		context.Background(),
		&record,
	)
}

func (s *DatabaseService) LoadScript(id string) (*models.ScriptRecord, error) {
	record, err := gorm.G[models.ScriptRecord](s.db).
		Where(&models.ScriptRecord{ID: id}).
		First(context.Background())

	if err != nil {
		return nil, err
	}
	return &record, nil
}

func (s *DatabaseService) FindAllScripts() ([]models.ScriptRecord, error) {
	return gorm.G[models.ScriptRecord](s.db).Find(context.Background())
}

func (s *DatabaseService) DeleteScript(id string) (int, error) {
	return gorm.G[models.ScriptRecord](s.db).
		Where(&models.ScriptRecord{ID: id}).
		Delete(context.Background())
}

func (s *DatabaseService) SetCache(key string, value string) error {
	record := models.KVCacheRecord{
		Key:   key,
		Value: value,
	}

	return gorm.G[models.KVCacheRecord](s.db, clause.OnConflict{UpdateAll: true}).
		Create(context.Background(), &record)
}

func (s *DatabaseService) GetCache(key string) (string, error) {
	record, err := gorm.G[models.KVCacheRecord](s.db).
		Where(&models.KVCacheRecord{Key: key}).
		First(context.Background())

	if err != nil {
		return "", nil
	}
	return record.Value, nil
}
