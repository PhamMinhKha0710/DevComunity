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
        _logger.LogInformation(
            "SignalR PushMessageAsync START: ConversationId={ConversationId}, MessageId={MessageId}",
            evt.ConversationId, evt.MessageId);
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

            _logger.LogInformation(
                "SignalR: Sent ReceiveMessage to conversation {ConversationId}, message {MessageId}",
                evt.ConversationId, evt.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push message {MessageId} via SignalR", evt.MessageId);
        }
    }

    public async Task PushNewMessageNotificationAsync(ChatPushEvent evt)
    {
        _logger.LogInformation(
            "SignalR PushNewMessageNotificationAsync START: ConversationId={ConversationId}, RecipientCount={RecipientCount}",
            evt.ConversationId, evt.RecipientUserIds.Count);
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

            _logger.LogInformation(
                "SignalR: Sent NewMessageNotification to {RecipientCount} users for conversation {ConversationId}",
                evt.RecipientUserIds.Count, evt.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push chat notification via SignalR");
        }
    }
}
