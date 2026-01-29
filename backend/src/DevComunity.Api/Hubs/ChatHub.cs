using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using DevComunity.Application.CommandHandlers.Chat;
using DevComunity.Application.Interfaces.Repositories;

namespace DevComunity.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time chat messaging
/// Persists messages to database and broadcasts to all participants
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ChatHub(ILogger<ChatHub> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private int GetCurrentUserId()
    {
        var userIdStr = Context.UserIdentifier;
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to ChatHub", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a conversation room
    /// </summary>
    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("User {UserId} joined conversation {ConversationId}", Context.UserIdentifier, conversationId);
    }

    /// <summary>
    /// Leave a conversation room
    /// </summary>
    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("User {UserId} left conversation {ConversationId}", Context.UserIdentifier, conversationId);
    }

    /// <summary>
    /// Send a message to a conversation - persists to DB and broadcasts
    /// </summary>
    public async Task SendMessage(int conversationId, string content)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            _logger.LogWarning("Unauthorized SendMessage attempt");
            return;
        }

        _logger.LogInformation("User {UserId} sending message to conversation {ConversationId}", userId, conversationId);

        try
        {
            // Persist message to database using scoped service
            using var scope = _serviceProvider.CreateScope();
            var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Verify user is part of conversation
            var conversation = await chatRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null || !conversation.Participants.Any(p => p.UserId == userId))
            {
                _logger.LogWarning("User {UserId} not authorized for conversation {ConversationId}", userId, conversationId);
                return;
            }

            // Get sender info
            var sender = await userRepository.GetByIdAsync(userId);
            
            // Create and save message
            var message = new DevComunity.Domain.Entities.Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = content,
                SentDate = DateTime.UtcNow,
                IsRead = false
            };

            var savedMessage = await chatRepository.AddMessageAsync(message);

            // Prepare message DTO for broadcast (matching frontend ChatMessage interface)
            var messageDto = new
            {
                messageId = savedMessage.MessageId,
                conversationId = savedMessage.ConversationId,
                senderId = savedMessage.SenderId, // int, not string
                senderName = sender?.DisplayName ?? sender?.Username ?? "Unknown",
                senderAvatar = sender?.ProfilePicture,
                content = savedMessage.Content,
                sentAt = savedMessage.SentDate.ToString("o"), // ISO 8601 format
                isRead = savedMessage.IsRead
            };

            _logger.LogInformation("Message {MessageId} saved, broadcasting to participants", 
                savedMessage.MessageId);

            // Only broadcast to user groups (not conversation group) to prevent duplicates
            // Each participant receives exactly ONE message through their personal group
            foreach (var participant in conversation.Participants)
            {
                _logger.LogInformation("Broadcasting message to user_{UserId}", participant.UserId);
                
                // Send ReceiveMessage to user's personal group
                await Clients.Group($"user_{participant.UserId}").SendAsync("ReceiveMessage", messageDto);
                
                // Also send notification for users who are not the sender
                if (participant.UserId != userId)
                {
                    await Clients.Group($"user_{participant.UserId}").SendAsync("NewMessageNotification", new
                    {
                        conversationId,
                        messagePreview = content.Length > 50 ? content.Substring(0, 50) + "..." : content,
                        senderName = sender?.DisplayName ?? sender?.Username
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message in conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Indicate typing status
    /// </summary>
    public async Task Typing(int conversationId, bool isTyping)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", new { userId = userId.ToString(), isTyping });
    }

    /// <summary>
    /// Mark messages as read
    /// </summary>
    public async Task MarkAsRead(int conversationId, int lastMessageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        try
        {
            // Persist read status to database
            using var scope = _serviceProvider.CreateScope();
            var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();
            await chatRepository.MarkMessagesAsReadAsync(conversationId, userId);

            // Notify others that messages were read
            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("MessagesRead", new { userId, lastMessageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read in conversation {ConversationId}", conversationId);
        }
    }
}
