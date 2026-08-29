package services

import (
	"context"
	"errors"
	"fmt"
	"io"
	"log/slog"
	"strings"
	"time"

	"github.com/wailsapp/wails/v3/pkg/application"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/utils"
	"github.com/ystreamutils/YStreamUtils/internal/youtube_protobuf"
	"google.golang.org/api/option"
	"google.golang.org/api/youtube/v3"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials"
	"google.golang.org/grpc/credentials/oauth"
)

type YouTubeService struct {
	Logger     *slog.Logger
	vault      *TokenVault
	apiService *youtube.Service
}

func (y *YouTubeService) SendChannelReply(liveChatID string, authorID string, replyText string) error {
	messageText := fmt.Sprintf("%s: %s", authorID, replyText)

	return y.SendMessage(liveChatID, messageText)
}

func (y *YouTubeService) SendMessage(liveChatID string, message string) error {
	messageDetails := &youtube.LiveChatTextMessageDetails{
		MessageText: message,
	}

	snippet := &youtube.LiveChatMessageSnippet{
		LiveChatId:         liveChatID,
		Type:               "textMessageEvent",
		TextMessageDetails: messageDetails,
	}

	liveChatMessage := &youtube.LiveChatMessage{
		Snippet: snippet,
	}

	call := y.apiService.LiveChatMessages.Insert([]string{"snippet"}, liveChatMessage)

	_, err := call.Do()
	if err != nil {
		return fmt.Errorf("failed to send chat message: %w", err)
	}
	return nil
}

func NewYouTubeService(ctx context.Context, v *TokenVault) (*YouTubeService, error) {
	service := &YouTubeService{
		Logger: utils.NewServiceLogger("YouTubeService"),
		vault:  v,
	}

	err := service.CreateClient(ctx)
	if err != nil {
		service.Logger.Error(err.Error())
	}
	return service, nil
}

func (y *YouTubeService) CreateClient(ctx context.Context) error {
	tokenSource, err := y.vault.GetTokenSource(models.PlatformYouTube)
	if err != nil {
		return fmt.Errorf("failed to initialize token source: %w", err)
	}

	svc, err := youtube.NewService(ctx, option.WithTokenSource(tokenSource))
	if err != nil {
		return fmt.Errorf("failed creating live google youtube api engine: %w", err)
	}

	y.apiService = svc
	return nil
}

func (y *YouTubeService) ConnectChat(ctx context.Context, videoId string) error {
	y.Logger.Info("Resolving video metadata and looking up active chat ID...", "videoId", videoId)

	chatID, err := y.GetLiveChatID(ctx, videoId)
	if err != nil {
		return fmt.Errorf("failed resolving active chat ID: %w", err)
	}

	tokenSource, err := y.vault.GetTokenSource(models.PlatformYouTube)
	if err != nil {
		return err
	}

	rpcCreds := oauth.TokenSource{
		TokenSource: tokenSource,
	}

	conn, err := grpc.NewClient(
		"dns:///youtube.googleapis.com:443",
		grpc.WithTransportCredentials(credentials.NewTLS(nil)),
		grpc.WithPerRPCCredentials(rpcCreds),
	)
	if err != nil {
		return fmt.Errorf("failed to dial YouTube gRPC stream cluster: %w", err)
	}

	client := youtube_protobuf.NewV3DataLiveChatMessageServiceClient(conn)

	go func() {
		defer conn.Close()
		var nextPageToken string

		baseBackoff := 1 * time.Second
		maxBackoff := 30 * time.Second
		currentBackoff := baseBackoff

		event := application.Get().Event

		for ctx.Err() == nil {
			req := &youtube_protobuf.LiveChatMessageListRequest{
				LiveChatId: &chatID,
				Part:       []string{"snippet", "authorDetails"},
			}
			if nextPageToken != "" {
				req.PageToken = &nextPageToken
			}

			stream, err := client.StreamList(ctx, req)
			if err != nil {
				y.Logger.Error("Failed initializing stream step chunk",
					slog.Any("error", err),
					slog.Duration("retry_delay", currentBackoff),
				)

				errStr := strings.ToLower(err.Error())
				if strings.Contains(errStr, "unauthenticated") ||
					strings.Contains(errStr, "permission_denied") ||
					strings.Contains(errStr, "quotaexceeded") ||
					strings.Contains(errStr, "resource_exhausted") {
					y.Logger.Error("Terminal authentication or quota restriction detected. Closing worker thread permanently.")
					return
				}

				select {
				case <-ctx.Done():
					return
				case <-time.After(currentBackoff):
				}
				currentBackoff = min(currentBackoff*2, maxBackoff)
				continue
			}
			y.Logger.Info("Successfully established live chat gRPC stream pipeline", "videoId", videoId)
			currentBackoff = baseBackoff

			for {
				if ctx.Err() != nil {
					return
				}

				chunk, err := stream.Recv()
				if err != nil {
					if errors.Is(err, io.EOF) {
						y.Logger.Debug("Stream reached 60s idle limit. Refreshing connection context smoothly...")
						currentBackoff = 0
					} else {
						y.Logger.Error("Real network drop or rate limit encountered", "error", err)
						currentBackoff = baseBackoff
					}
					break
				}

				if token := chunk.GetNextPageToken(); token != "" {
					if token != nextPageToken {
						y.Logger.Info("Updated internal stream page cursor bounds", "videoId", videoId)
						nextPageToken = token
					}
				}

				items := chunk.GetItems()
				if len(items) > 0 {
					currentBackoff = baseBackoff

					for _, item := range items {
						y.processIncomingItem(item, event)
					}
				}
			}

			select {
			case <-ctx.Done():
				return
			case <-time.After(currentBackoff):
			}
		}
	}()

	return nil
}

func (y *YouTubeService) GetConcurrentViewers(ctx context.Context, channelID string) (int64, error) {
	if y.apiService == nil {
		return 0, errors.New("unauthenticated client context")
	}

	call := y.apiService.Videos.List([]string{"liveStreamingDetails"}).Id(channelID)
	response, err := call.Context(ctx).Do()
	if err != nil {
		return 0, err
	}

	if len(response.Items) == 0 {
		return 0, fmt.Errorf("no live assets found for id: %s", channelID)
	}

	video := response.Items[0]
	if video.LiveStreamingDetails == nil {
		return 0, nil
	}

	return int64(video.LiveStreamingDetails.ConcurrentViewers), nil
}

func (y *YouTubeService) GetProfile(ctx context.Context) (*models.UserProfile, error) {
	if y.apiService == nil {
		return nil, errors.New("unauthenticated client context")
	}

	call := y.apiService.Channels.List([]string{"snippet", "id"}).Mine(true)
	response, err := call.Context(ctx).Do()
	if err != nil {
		return nil, err
	}

	if len(response.Items) == 0 {
		return nil, nil
	}

	channel := response.Items[0]

	y.Logger.Info("YouTube channel data successfully retrieved and parsed",
		slog.String("channel_id", channel.Id),
	)

	return &models.UserProfile{
		DisplayName: channel.Snippet.Title,
		AvatarURL:   channel.Snippet.Thumbnails.Default.Url,
		ChannelID:   channel.Id,
		Handle:      channel.Snippet.CustomUrl,
	}, nil
}

func (y *YouTubeService) FindActiveBroadcastVideoIDs(ctx context.Context, includeUpcoming bool) ([]string, error) {
	if y.apiService == nil {
		return nil, errors.New("unauthenticated client context")
	}

	call := y.apiService.LiveBroadcasts.
		List([]string{"id", "snippet", "status"}).
		BroadcastType("all").
		Mine(true)

	response, err := call.Context(ctx).Do()
	if err != nil {
		y.Logger.Error("failed to list youtube live broadcasts", "error", err.Error())
		return nil, err
	}

	var liveVideoIDs []string
	for _, broadcast := range response.Items {
		if broadcast.Status == nil || broadcast.Snippet == nil {
			continue
		}

		status := broadcast.Status.LifeCycleStatus

		isLive := status == "live" && broadcast.Snippet.LiveChatId != ""

		isUpcoming := includeUpcoming && status == "upcoming"

		if isLive || isUpcoming {
			liveVideoIDs = append(liveVideoIDs, broadcast.Id)
		}
	}

	y.Logger.Info("active broadcast evaluation loop complete", "detected_count", len(liveVideoIDs))
	return liveVideoIDs, nil
}

func (y *YouTubeService) GetLiveChatID(ctx context.Context, videoID string) (string, error) {
	if y.apiService == nil {
		return "", errors.New("unauthenticated API client")
	}

	call := y.apiService.Videos.List([]string{"liveStreamingDetails"}).Id(videoID)
	response, err := call.Context(ctx).Do()
	if err != nil {
		return "", fmt.Errorf("failed to fetch video details: %w", err)
	}

	if len(response.Items) == 0 {
		return "", fmt.Errorf("no video found with ID: %s", videoID)
	}

	video := response.Items[0]
	if video.LiveStreamingDetails == nil || video.LiveStreamingDetails.ActiveLiveChatId == "" {
		return "", fmt.Errorf("video %s does not have an active live chat", videoID)
	}

	return video.LiveStreamingDetails.ActiveLiveChatId, nil
}

func (y *YouTubeService) processIncomingItem(item *youtube_protobuf.LiveChatMessage, event *application.EventManager) {
	if item.Snippet == nil {
		return
	}
	y.Logger.Debug(*item.Id)

	author := ""
	if item.AuthorDetails != nil {
		author = item.AuthorDetails.GetDisplayName()
	}

	switch item.Snippet.GetType() {

	case youtube_protobuf.LiveChatMessageSnippet_TypeWrapper_SUPER_CHAT_EVENT:
		scMsg := models.StreamSuperchatMessageEvent{
			StreamChatMessageEvent: models.StreamChatMessageEvent{
				BaseUserData: models.BaseUserData{
					Message:     item.Snippet.GetDisplayMessage(),
					MessageID:   item.GetId(),
					Author:      author,
					AuthorID:    *item.GetSnippet().AuthorChannelId,
					AuthorColor: GetYouTubeUserColor(item),
				},
				LiveChatID: *item.Snippet.LiveChatId,
			},
			Amount: *item.Snippet.GetSuperChatDetails().AmountDisplayString,
		}

		evt := models.NewStreamEvent(models.StreamSuperchatMessage, models.PlatformYouTube, scMsg)
		event.Emit(string(models.EventKeyStreamChatMessage), evt)
		event.Emit(string(models.EventKeyYoutubeSuperchat), evt)

	case youtube_protobuf.LiveChatMessageSnippet_TypeWrapper_TEXT_MESSAGE_EVENT:
		fallthrough

	default:
		chatMsg := models.StreamChatMessageEvent{
			BaseUserData: models.BaseUserData{
				Message:     item.Snippet.GetDisplayMessage(),
				MessageID:   item.GetId(),
				Author:      author,
				AuthorID:    *item.GetSnippet().AuthorChannelId,
				AuthorColor: GetYouTubeUserColor(item),
			},
			LiveChatID: *item.Snippet.LiveChatId,
		}

		event.Emit(string(models.EventKeyStreamChatMessage), models.NewStreamEvent(models.StreamChatMessage, models.PlatformYouTube, chatMsg))
	}

}

func GetYouTubeUserColor(item *youtube_protobuf.LiveChatMessage) string {
	if item.AuthorDetails.GetIsChatOwner() {
		return "#e6b800"
	}

	if item.AuthorDetails.GetIsChatModerator() {
		return "#5e97ff"
	}

	if item.AuthorDetails.GetIsChatSponsor() {
		return "#107c10"
	}

	return ""
}
