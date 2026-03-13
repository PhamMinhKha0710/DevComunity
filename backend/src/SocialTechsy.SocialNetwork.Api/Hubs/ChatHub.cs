using System.Text.Encodings.Web;
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

    private readonly IMediator _mediator;
    private readonly ILogger<ChatHub> _logger;
    private readonly RedisChatRateLimiter? _rateLimiter;

    public ChatHub(IMediator mediator, ILogger<ChatHub> logger, RedisChatRateLimiter? rateLimiter = null)
    {
        _mediator = mediator;
        _logger = logger;
        _rateLimiter = rateLimiter;
    }

    private int GetCurrentUserId() =>
        int.TryParse(Context.UserIdentifier, out var id) ? id : 0;

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

    public async Task SendMessage(int conversationId, string content, int? replyToMessageId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        if (string.IsNullOrWhiteSpace(content))
        { await SendError("Message content cannot be empty"); return; }
        if (content.Length > MaxMessageLength)
        { await SendError($"Message exceeds maximum length of {MaxMessageLength} characters"); return; }
        if (!await IsRateLimitAllowedAsync(userId))
        { await SendError("Rate limit exceeded. Please slow down."); return; }

        var result = await _mediator.Send(new SendMessageCommand
        {
            ConversationId = conversationId,
            SenderId = userId,
            Content = HtmlEncoder.Default.Encode(content),
            ReplyToMessageId = replyToMessageId
        });

        if (result == null) return;
        await BroadcastMessageAsync(conversationId, result);
    }

    public async Task SendMediaMessage(int conversationId, string messageType, string attachmentUrl,
        string attachmentFileName, long attachmentSize, string? caption = null, int? replyToMessageId = null)
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

        var result = await _mediator.Send(new SendMessageCommand
        {
            ConversationId = conversationId,
            SenderId = userId,
            Content = caption != null ? HtmlEncoder.Default.Encode(caption) : "",
            MessageType = parsedMessageType,
            AttachmentUrl = attachmentUrl,
            AttachmentFileName = attachmentFileName,
            AttachmentSize = attachmentSize,
            ReplyToMessageId = replyToMessageId
        });

        if (result == null) return;
        await BroadcastMessageAsync(conversationId, result);
    }

    public async Task SyncMessages(int conversationId, int lastMessageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        var messages = await _mediator.Send(new GetMessagesSinceQuery
        {
            ConversationId = conversationId,
            UserId = userId,
            SinceMessageId = lastMessageId
        });

        await Clients.Caller.SendAsync("SyncMessages", new { conversationId, messages });
    }

    public async Task Typing(int conversationId, bool isTyping)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", new { userId = userId.ToString(), isTyping });
    }

    public async Task MarkAsRead(int conversationId, int lastMessageId)
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

    public async Task AddReaction(int conversationId, int messageId, string reactionType)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

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
            .SendAsync("ReceiveReaction", result);
    }

    public async Task AcknowledgeDelivery(int conversationId, int messageId, string status)
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

    public async Task RemoveReaction(int conversationId, int messageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        var removed = await _mediator.Send(new RemoveReactionCommand
        {
            MessageId = messageId,
            UserId = userId
        });

        if (!removed) return;
        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("RemoveReaction", new { messageId, userId });
    }

    private async Task BroadcastMessageAsync(int conversationId, SendMessageResult result)
    {
        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("ReceiveMessage", result.Message);

        var notification = new
        {
            conversationId,
            messagePreview = result.NotificationPreview,
            senderName = result.SenderDisplayName
        };
        var tasks = result.OtherParticipantUserIds
            .Select(uid => Clients.Group($"user_{uid}").SendAsync("NewMessageNotification", notification));
        await Task.WhenAll(tasks);
    }

    private async Task<bool> IsRateLimitAllowedAsync(int userId) =>
        _rateLimiter == null || await _rateLimiter.IsAllowedAsync(userId, MaxMessagesPerMinute);

    private Task SendError(string message) =>
        Clients.Caller.SendAsync("Error", message);
}
