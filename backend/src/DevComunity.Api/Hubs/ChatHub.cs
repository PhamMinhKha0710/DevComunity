using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DevComunity.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time chat messaging
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
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
    /// Send a message to a conversation
    /// </summary>
    public async Task SendMessage(int conversationId, string content)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} sent message to conversation {ConversationId}", userId, conversationId);

        var message = new
        {
            messageId = Guid.NewGuid().ToString(),
            conversationId,
            senderId = userId,
            content,
            sentAt = DateTime.UtcNow
        };

        // Broadcast to all users in the conversation
        await Clients.Group($"conversation_{conversationId}").SendAsync("ReceiveMessage", message);
    }

    /// <summary>
    /// Indicate typing status
    /// </summary>
    public async Task Typing(int conversationId, bool isTyping)
    {
        var userId = Context.UserIdentifier;
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", new { userId, isTyping });
    }

    /// <summary>
    /// Mark messages as read
    /// </summary>
    public async Task MarkAsRead(int conversationId, int lastMessageId)
    {
        var userId = Context.UserIdentifier;
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("MessagesRead", new { userId, lastMessageId });
    }
}
