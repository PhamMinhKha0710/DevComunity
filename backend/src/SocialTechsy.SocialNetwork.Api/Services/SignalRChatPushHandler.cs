using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Api.Hubs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Api.Services;

public class SignalRChatPushHandler : IChatPushHandler
{
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly ILogger<SignalRChatPushHandler> _logger;

    public SignalRChatPushHandler(
        IHubContext<ChatHub> chatHub,
        ILogger<SignalRChatPushHandler> logger)
    {
        _chatHub = chatHub;
        _logger = logger;
    }

    public async Task PushMessageAsync(ChatPushEvent evt)
    {
        try
        {
            await _chatHub.Clients.Group($"conversation_{evt.ConversationId}")
                .SendAsync("ReceiveMessage", new
                {
                    evt.MessageId,
                    evt.ConversationId,
                    evt.SenderId,
                    SenderUsername = evt.SenderUsername,
                    SenderProfilePicture = evt.SenderProfilePicture,
                    evt.Content,
                    evt.MessageType,
                    evt.AttachmentUrl,
                    evt.AttachmentFileName,
                    evt.AttachmentSize,
                    evt.SentDate,
                    IsRead = false,
                    evt.ReplyToMessageId,
                });

            _logger.LogDebug("Pushed message {MessageId} to conversation {ConversationId}",
                evt.MessageId, evt.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push message {MessageId} via SignalR", evt.MessageId);
        }
    }

    public async Task PushNewMessageNotificationAsync(ChatPushEvent evt)
    {
        try
        {
            var notification = new
            {
                conversationId = evt.ConversationId,
                messagePreview = evt.NotificationPreview,
                senderName = evt.SenderDisplayName ?? evt.SenderUsername
            };

            var tasks = evt.RecipientUserIds
                .Select(uid => _chatHub.Clients.Group($"user_{uid}")
                    .SendAsync("NewMessageNotification", notification));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push chat notification via SignalR");
        }
    }
}
