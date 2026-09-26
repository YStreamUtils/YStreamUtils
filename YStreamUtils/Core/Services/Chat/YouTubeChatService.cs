using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Youtube.Api.V3;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Exceptions;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services.YouTube;
using YStreamUtils.Extensions;


namespace YStreamUtils.Core.Services.Chat;

public class YouTubeChatService(
    IServiceProvider serviceProvider,
    IEventBus eventBus,
    ChatManager chatManager,
    ILogger<YouTubeChatService> logger) : IChatService
{

    private const Platform CurrentPlatform = Platform.YouTube;
    private async Task<string?> GetChatId(string videoId, CancellationToken token)
    {
        using var scope = serviceProvider.CreateScope();
        var client = await scope.ServiceProvider.GetRequiredService<YouTubeCredentialService>()
            .GetClient(false);
        if (client == null) throw new Exception("YouTube credential not found");

        var req = client.Videos.List("liveStreamingDetails");
        req.Id = videoId;

        var res = await req.ExecuteAsync(token);
        return res.Items.Count == 0
            ? throw new StreamNotFoundException("Aborting stream setup: No streams listed on account.")
            : res.Items?.FirstOrDefault()?.LiveStreamingDetails?.ActiveLiveChatId;
    }

    public async Task StartChatStream(string videoId, CancellationToken token = default)
    {
        if (chatManager.IsChatStreamRunning(CurrentPlatform, videoId)) return;

        var chatId = await GetChatId(videoId, token);
        if (string.IsNullOrEmpty(chatId))
        {
            throw new ChatNotFoundException($"Aborting stream setup: No active chat ID found for video {videoId}.");
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        chatManager.RegisterChatStream(CurrentPlatform, videoId, cts);

        _ = Task.Run(() => RunStreamLoopAsync(chatId, cts.Token), cts.Token);
    }

    public void StopChatStream(string videoId)
    {
        chatManager.UnregisterChatStream(CurrentPlatform, videoId);
    }

    private async Task RunStreamLoopAsync(string chatId, CancellationToken token)
    {
        logger.LogInformation("Starting YouTube stream loop for Chat: {ChatId}", chatId);
        string? nextPageToken = null;

        while (!token.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var credentialService = scope.ServiceProvider.GetRequiredService<YouTubeCredentialService>();

                var credential = await credentialService.GetClient(false);

                if (credential.HttpClientInitializer is not Google.Apis.Auth.OAuth2.UserCredential userCred)
                {
                    logger.LogWarning(
                        "Credentials are not a valid UserCredential mapping instance.");
                    await Task.Delay(30000, token);
                    continue;
                }

                var authResult = await userCred.GetAccessTokenForRequestAsync(cancellationToken: token);

                using var channel = GrpcChannel.ForAddress("https://googleapis.com");
                var client = new V3DataLiveChatMessageService.V3DataLiveChatMessageServiceClient(channel);

                var headers = new Metadata { { "Authorization", $"Bearer {authResult}" } };
                var request = new LiveChatMessageListRequest { LiveChatId = chatId };

                if (!string.IsNullOrEmpty(nextPageToken)) request.PageToken = nextPageToken;

                using var streamingCall = client.StreamList(request, headers, cancellationToken: token);

                await foreach (var response in streamingCall.ResponseStream.ReadAllAsync(token))
                {
                    nextPageToken = response.NextPageToken;

                    foreach (var message in response.Items)
                    {
                        if (message?.Snippet is null) continue;
                        var authorId = message.AuthorDetails?.ChannelId ?? string.Empty;
                        var authorName = message.AuthorDetails?.DisplayName ?? "Anonymous";
                        var messageId = message.Id ?? string.Empty;
                        var displayMessage = message.Snippet?.DisplayMessage ?? string.Empty;
                        const string authorColor = "#FFFFFF";

                        var user = new BaseUserData(
                            authorId,
                            authorName,
                            authorColor
                        );

                        switch (message.Snippet?.Type)
                        {
                            case LiveChatMessageSnippet.Types.TypeWrapper.Types.Type.SuperChatEvent
                                when message.Snippet.SuperChatDetails is not null:
                            {
                                var superchatData = new StreamSuperChatMessageEvent(
                                    User: user,
                                    MessageId: messageId,
                                    Message: displayMessage,
                                    LiveChatId: chatId,
                                    Amount: message.Snippet.SuperChatDetails.AmountDisplayString ?? "0.00"
                                );

                                var envelope = StreamEventEnvelope<StreamSuperChatMessageEvent>.Create(
                                    Platform.YouTube,
                                    superchatData
                                );

                                await eventBus.PublishAsync(EventKey.StreamChatMessage, envelope, token);
                                break;
                            }

                            case LiveChatMessageSnippet.Types.TypeWrapper.Types.Type.TextMessageEvent:
                            case LiveChatMessageSnippet.Types.TypeWrapper.Types.Type.MemberMilestoneChatEvent:
                            {
                                var chatData = new StreamChatMessageEvent(
                                    User: user,
                                    MessageId: messageId,
                                    Message: displayMessage,
                                    LiveChatId: chatId
                                );

                                var envelope = StreamEventEnvelope<StreamChatMessageEvent>.Create(
                                    Platform.YouTube,
                                    chatData
                                );

                                await eventBus.PublishAsync(EventKey.StreamChatMessage, envelope, token);
                                break;
                            }

                            default:
                                break;
                        }
                    }
                }
            }
            catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.Unauthenticated)
            {
                logger.LogWarning("Stream connection unauthenticated for chat {ChatId}. Refreshing token...",
                    chatId);
                await Task.Delay(2000, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Streaming error on Chat {ChatId}. Reconnecting in 5s...", chatId);
                await Task.Delay(5000, token);
            }
        }

        logger.LogInformation("Successfully shut down stream thread for Chat: {ChatId}", chatId);
    }
}