using static Youtube.Api.V3.LiveChatMessageSnippet.Types.TypeWrapper.Types;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Youtube.Api.V3;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;


namespace YStreamUtils.Core.Services.YouTube;

public class YouTubeChatService(
    IServiceProvider serviceProvider,
    IEventBus eventBus,
    YouTubeStreamManager streamManager,
    ILogger<YouTubeChatService> logger)
{
    private async Task<string?> GetChatId(string tenantId, string videoId, bool isBot, CancellationToken token)
    {
        using var scope = serviceProvider.CreateScope();
        var client = await scope.ServiceProvider.GetRequiredService<YouTubeCredentialService>()
            .GetClient(tenantId, isBot);
        if (client == null) return null;

        var req = client.Videos.List("liveStreamingDetails");
        req.Id = videoId;

        var res = await req.ExecuteAsync(token);
        return res.Items?.FirstOrDefault()?.LiveStreamingDetails?.ActiveLiveChatId;
    }

    public async Task StartStream(string tenantId, string videoId, bool isBot, CancellationToken token = default)
    {
        if (streamManager.IsStreamRunning(tenantId, videoId)) return;

        var chatId = await GetChatId(tenantId, videoId, isBot, token);
        if (string.IsNullOrEmpty(chatId))
        {
            logger.LogWarning("Aborting stream setup: No active chat ID found for video {VideoId}.", videoId);
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        streamManager.RegisterStream(tenantId, videoId, cts);

        _ = Task.Run(() => RunStreamLoopAsync(tenantId, chatId, isBot, cts.Token), cts.Token);
    }

    public void StopStream(string tenantId, string videoId)
    {
        streamManager.UnregisterStream(tenantId, videoId);
    }

    private async Task RunStreamLoopAsync(string tenantId, string chatId, bool isBot, CancellationToken token)
    {
        logger.LogInformation("Starting YouTube stream loop for Tenant: {TenantId}, Chat: {ChatId}", tenantId, chatId);
        string? nextPageToken = null;

        while (!token.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var credentialService = scope.ServiceProvider.GetRequiredService<YouTubeCredentialService>();

                var credential = await credentialService.GetClient(tenantId, isBot);
                if (credential == null)
                {
                    logger.LogWarning("Missing client credentials for Tenant {TenantId}. Retrying in 30s...", tenantId);
                    await Task.Delay(30000, token);
                    continue;
                }

                if (credential.HttpClientInitializer is not Google.Apis.Auth.OAuth2.UserCredential userCred)
                {
                    logger.LogWarning(
                        "Credentials for Tenant {TenantId} are not a valid UserCredential mapping instance.", tenantId);
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
                                    tenantId,
                                    StreamEventName.Superchat,
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
                                    tenantId,
                                    StreamEventName.Chat,
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
                logger.LogWarning("Stream connection unauthenticated for tenant {TenantId}. Refreshing token...",
                    tenantId);
                await Task.Delay(2000, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Streaming error on Tenant {TenantId}. Reconnecting in 5s...", tenantId);
                await Task.Delay(5000, token);
            }
        }

        logger.LogInformation("Successfully shut down stream thread for Tenant: {TenantId}", tenantId);
    }
}