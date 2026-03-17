using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Chat;
using SocialTechsy.SocialNetwork.Domain.Enums;
using SocialTechsy.SocialNetwork.Infrastructure.Redis;

namespace SocialTechsy.SocialNetwork.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private const int MaxMessageLength = 10_000;
    private const int MaxMessagesPerMinute = 30;
    private static readonly TimeSpan TypingThrottleWindow = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan TypingTtl = TimeSpan.FromSeconds(5);

    private readonly IMediator _mediator;
    private readonly ILogger<ChatHub> _logger;
    private readonly RedisChatRateLimiter? _rateLimiter;
    private readonly RedisChatCacheService? _chatCache;

    public ChatHub(IMediator mediator, ILogger<ChatHub> logger,
        RedisChatRateLimiter? rateLimiter = null, RedisChatCacheService? chatCache = null)
    {
        _mediator = mediator;
        _logger = logger;
        _rateLimiter = rateLimiter;
        _chatCache = chatCache;
    }

    private int GetCurrentUserId() =>
        int.TryParse(Context.UserIdentifier, out var id) ? id : 0;

    /// <summary>
    /// Escapes only dangerous HTML chars while preserving Unicode (Vietnamese, emoji, etc.).
    /// Unlike HtmlEncoder.Default.Encode which converts non-ASCII to HTML numeric entities.
    /// </summary>
    private static string SanitizeHtml(string input) =>
        WebUtility.HtmlEncode(input);

    public override async Task OnConnectedAsync()
    {
        if (Context.UserIdentifier is { } uid)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{uid}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.UserIdentifier is { } uid)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{uid}");
        await base.OnDisconnectedAsync(exception);
    }

    public Task JoinConversation(int conversationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");

    public Task LeaveConversation(int conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");

    public async Task SendMessage(int conversationId, string content, string? replyToMessageId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        if (string.IsNullOrWhiteSpace(content))
        { await SendError("Message content cannot be empty"); return; }
        if (content.Length > MaxMessageLength)
        { await SendError($"Message exceeds maximum length of {MaxMessageLength} characters"); return; }
        if (!await IsRateLimitAllowedAsync(userId))
        { await SendError("Rate limit exceeded. Please slow down."); return; }

        long? replyId = null;
        if (!string.IsNullOrEmpty(replyToMessageId) && long.TryParse(replyToMessageId, out var parsedReplyId))
        {
            replyId = parsedReplyId;
        }

        var result = await _mediator.Send(new SendMessageCommand
        {
            ConversationId = conversationId,
            SenderId = userId,
            Content = SanitizeHtml(content),
            ReplyToMessageId = replyId
        });

        if (result == null) return;
        await BroadcastMessageAsync(conversationId, result);
    }

    public async Task SendMediaMessage(int conversationId, string messageType, string attachmentUrl,
        string attachmentFileName, long attachmentSize, string? caption = null, string? replyToMessageId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        if (!Enum.TryParse<MessageType>(messageType, ignoreCase: true, out var parsedMessageType))
        { await SendError("Invalid message type"); return; }
        if (string.IsNullOrWhiteSpace(attachmentUrl))
        { await SendError("Attachment URL is required"); return; }
        if (caption != null && caption.Length > MaxMessageLength)
        { await SendError($"Caption exceeds maximum length of {MaxMessageLength} characters"); return; }
        if (!await IsRateLimitAllowedAsync(userId))
        { await SendError("Rate limit exceeded. Please slow down."); return; }

        long? replyId = null;
        if (!string.IsNullOrEmpty(replyToMessageId) && long.TryParse(replyToMessageId, out var parsedReplyId))
        {
            replyId = parsedReplyId;
        }

        var result = await _mediator.Send(new SendMessageCommand
        {
            ConversationId = conversationId,
            SenderId = userId,
            Content = caption != null ? SanitizeHtml(caption) : "",
            MessageType = parsedMessageType,
            AttachmentUrl = attachmentUrl,
            AttachmentFileName = attachmentFileName,
            AttachmentSize = attachmentSize,
            ReplyToMessageId = replyId
        });

        if (result == null) return;
        await BroadcastMessageAsync(conversationId, result);
    }

    public async Task SyncMessages(int conversationId, string lastMessageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        if (!long.TryParse(lastMessageId, out var sinceId))
            return;

        var messages = await _mediator.Send(new GetMessagesSinceQuery
        {
            ConversationId = conversationId,
            UserId = userId,
            SinceMessageId = sinceId
        });

        await Clients.Caller.SendAsync("SyncMessages", new { conversationId, messages });
    }

    public async Task Typing(int conversationId, bool isTyping)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        if (_chatCache != null)
        {
            if (isTyping)
            {
                var allowed = await _chatCache.SetTypingThrottleAsync(userId, conversationId, TypingThrottleWindow);
                if (!allowed) return;
                await _chatCache.SetTypingAsync(userId, conversationId, TypingTtl);
            }
            else
            {
                await _chatCache.ClearTypingAsync(userId, conversationId);
            }
        }

        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", new { userId = userId.ToString(), isTyping });
    }

    public async Task MarkAsRead(int conversationId, long lastMessageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        await _mediator.Send(new MarkConversationReadCommand
        {
            ConversationId = conversationId,
            UserId = userId
        });

        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("MessagesRead", new { userId, lastMessageId });
    }

    public async Task AddReaction(int conversationId, string messageIdStr, string reactionType)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        if (!long.TryParse(messageIdStr, out var messageId))
        { await SendError("Invalid message ID"); return; }

        if (!Enum.TryParse<ReactionType>(reactionType, ignoreCase: true, out var parsedReaction))
        { await SendError("Invalid reaction type"); return; }

        var result = await _mediator.Send(new AddReactionCommand
        {
            MessageId = messageId,
            UserId = userId,
            ReactionType = parsedReaction
        });

        if (result == null) return;
        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("ReceiveReaction", new
            {
                messageId,
                result.UserId,
                result.Username,
                result.ProfilePicture,
                result.ReactionType,
                result.CreatedAt
            });
    }

    public async Task AcknowledgeDelivery(int conversationId, long messageId, string status)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        if (!Enum.TryParse<SocialTechsy.SocialNetwork.Domain.Entities.DeliveryStatus>(status, true, out var deliveryStatus))
            return;

        var updated = await _mediator.Send(new AcknowledgeDeliveryCommand
        {
            MessageId = messageId,
            UserId = userId,
            Status = deliveryStatus
        });

        if (!updated) return;
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("DeliveryStatusUpdated", new { messageId, userId, status = status.ToLower() });
    }

    public async Task RemoveReaction(int conversationId, string messageIdStr)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        if (!long.TryParse(messageIdStr, out var messageId))
        { await SendError("Invalid message ID"); return; }

        var removed = await _mediator.Send(new RemoveReactionCommand
        {
            MessageId = messageId,
            UserId = userId
        });

        if (!removed) return;
        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("RemoveReaction", new { messageId, userId });
    }

    public async Task EditMessage(int conversationId, int messageId, string newContent)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        if (string.IsNullOrWhiteSpace(newContent))
        { await SendError("Message content cannot be empty"); return; }
        if (newContent.Length > MaxMessageLength)
        { await SendError($"Message exceeds maximum length of {MaxMessageLength} characters"); return; }

        var result = await _mediator.Send(new EditMessageCommand
        {
            MessageId = messageId,
            UserId = userId,
            NewContent = SanitizeHtml(newContent)
        });

        if (!result)
        { await SendError("Cannot edit this message. You may not be the sender or the edit window has expired."); return; }

        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("MessageEdited", new { messageId, newContent, editedDate = DateTime.UtcNow });
    }

    public async Task DeleteMessage(int conversationId, int messageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        var result = await _mediator.Send(new DeleteMessageCommand
        {
            MessageId = messageId,
            UserId = userId
        });

        if (!result)
        { await SendError("Cannot delete this message."); return; }

        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("MessageDeleted", new { messageId, deletedAt = DateTime.UtcNow });
    }

    private async Task BroadcastMessageAsync(int conversationId, SendMessageResult result)
    {
        if (result.GroupTier == "large")
        {
            var badgeNotification = new
            {
                conversationId,
                type = "new_activity",
                senderName = result.SenderDisplayName
            };
            var tasks = result.OtherParticipantUserIds
                .Select(uid => Clients.Group($"user_{uid}").SendAsync("NewActivityNotification", badgeNotification));
            await Task.WhenAll(tasks);
            return;
        }

        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("ReceiveMessage", result.Message);

        // NewMessageNotification is handled by the outbox/RabbitMQ pipeline
        // (ChatMessageConsumerService -> SignalRChatPushHandler) to avoid duplicate
        // delivery when both direct hub and async consumer are active.
    }

    private async Task<bool> IsRateLimitAllowedAsync(int userId) =>
        _rateLimiter == null || await _rateLimiter.IsAllowedAsync(userId, MaxMessagesPerMinute);

    private Task SendError(string message) =>
        Clients.Caller.SendAsync("Error", message);
}