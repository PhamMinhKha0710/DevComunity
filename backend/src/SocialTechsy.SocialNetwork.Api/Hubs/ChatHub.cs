using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Api.Hubs;

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
    public async Task SendMessage(int conversationId, string content, int? replyToMessageId = null)
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
            var message = new SocialTechsy.SocialNetwork.Domain.Entities.Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = content,
                SentDate = DateTime.UtcNow,
                IsRead = false,
                ReplyToMessageId = replyToMessageId
            };

            var savedMessage = await chatRepository.AddMessageAsync(message);

            // Get reply message info if applicable
            object? replyToDto = null;
            if (replyToMessageId.HasValue)
            {
                var replyToMessage = await chatRepository.GetMessageByIdAsync(replyToMessageId.Value);
                if (replyToMessage != null)
                {
                    replyToDto = new
                    {
                        messageId = replyToMessage.MessageId,
                        senderId = replyToMessage.SenderId,
                        senderUsername = replyToMessage.Sender?.Username ?? "",
                        content = replyToMessage.Content.Length > 100 
                            ? replyToMessage.Content.Substring(0, 100) + "..." 
                            : replyToMessage.Content
                    };
                }
            }

            // Prepare message DTO for broadcast (matching frontend ChatMessage interface)
            var messageDto = new
            {
                messageId = savedMessage.MessageId,
                conversationId = savedMessage.ConversationId,
                senderId = savedMessage.SenderId,
                senderUsername = sender?.Username ?? "Unknown",
                senderProfilePicture = sender?.ProfilePicture,
                content = savedMessage.Content,
                messageType = savedMessage.MessageType ?? "text",
                sentDate = savedMessage.SentDate.ToString("o"),
                isRead = savedMessage.IsRead,
                replyToMessageId = savedMessage.ReplyToMessageId,
                replyToMessage = replyToDto
            };

            _logger.LogInformation("Message {MessageId} saved, broadcasting to participants", 
                savedMessage.MessageId);

            var sendTasks = new List<Task>();
            foreach (var participant in conversation.Participants)
            {
                sendTasks.Add(Clients.Group($"user_{participant.UserId}").SendAsync("ReceiveMessage", messageDto));

                if (participant.UserId != userId)
                {
                    sendTasks.Add(Clients.Group($"user_{participant.UserId}").SendAsync("NewMessageNotification", new
                    {
                        conversationId,
                        messagePreview = content.Length > 50 ? content.Substring(0, 50) + "..." : content,
                        senderName = sender?.DisplayName ?? sender?.Username
                    }));
                }
            }
            await Task.WhenAll(sendTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message in conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Send a media message (image, video, audio, file) to a conversation
    /// </summary>
    public async Task SendMediaMessage(int conversationId, string messageType, string attachmentUrl, string attachmentFileName, long attachmentSize, string? caption = null, int? replyToMessageId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            _logger.LogWarning("Unauthorized SendMediaMessage attempt");
            return;
        }

        _logger.LogInformation("User {UserId} sending media message ({Type}) to conversation {ConversationId}", 
            userId, messageType, conversationId);

        try
        {
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
            
            // Create and save media message
            var message = new SocialTechsy.SocialNetwork.Domain.Entities.Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = caption ?? "", // Caption is optional
                MessageType = messageType,
                AttachmentUrl = attachmentUrl,
                AttachmentFileName = attachmentFileName,
                AttachmentSize = attachmentSize,
                SentDate = DateTime.UtcNow,
                IsRead = false,
                ReplyToMessageId = replyToMessageId
            };

            var savedMessage = await chatRepository.AddMessageAsync(message);

            // Get reply message info if applicable
            object? replyToDto = null;
            if (replyToMessageId.HasValue)
            {
                var replyToMessage = await chatRepository.GetMessageByIdAsync(replyToMessageId.Value);
                if (replyToMessage != null)
                {
                    replyToDto = new
                    {
                        messageId = replyToMessage.MessageId,
                        senderId = replyToMessage.SenderId,
                        senderUsername = replyToMessage.Sender?.Username ?? "",
                        content = replyToMessage.Content.Length > 100 
                            ? replyToMessage.Content.Substring(0, 100) + "..." 
                            : replyToMessage.Content
                    };
                }
            }

            // Prepare message DTO for broadcast (matching frontend ChatMessage interface)
            var messageDto = new
            {
                messageId = savedMessage.MessageId,
                conversationId = savedMessage.ConversationId,
                senderId = savedMessage.SenderId,
                senderUsername = sender?.Username ?? "Unknown",
                senderProfilePicture = sender?.ProfilePicture,
                content = savedMessage.Content,
                messageType = savedMessage.MessageType,
                attachmentUrl = savedMessage.AttachmentUrl,
                attachmentFileName = savedMessage.AttachmentFileName,
                attachmentSize = savedMessage.AttachmentSize,
                sentDate = savedMessage.SentDate.ToString("o"),
                isRead = savedMessage.IsRead,
                replyToMessageId = savedMessage.ReplyToMessageId,
                replyToMessage = replyToDto
            };

            _logger.LogInformation("Media message {MessageId} saved, broadcasting to participants", 
                savedMessage.MessageId);

            var mediaTasks = new List<Task>();
            foreach (var participant in conversation.Participants)
            {
                mediaTasks.Add(Clients.Group($"user_{participant.UserId}").SendAsync("ReceiveMessage", messageDto));

                if (participant.UserId != userId)
                {
                    var preview = messageType == "image" ? "Photo"
                        : messageType == "video" ? "Video"
                        : messageType == "audio" ? "Audio"
                        : attachmentFileName ?? "File";

                    mediaTasks.Add(Clients.Group($"user_{participant.UserId}").SendAsync("NewMessageNotification", new
                    {
                        conversationId,
                        messagePreview = preview,
                        senderName = sender?.DisplayName ?? sender?.Username
                    }));
                }
            }
            await Task.WhenAll(mediaTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending media message in conversation {ConversationId}", conversationId);
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

    /// <summary>
    /// Add or update a reaction to a message (Instagram/Facebook style)
    /// </summary>
    public async Task AddReaction(int conversationId, int messageId, string reactionType)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        // Validate reaction type
        var validReactions = new[] { "like", "love", "haha", "wow", "sad", "angry" };
        if (!validReactions.Contains(reactionType.ToLower())) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Get user info
            var user = await userRepository.GetByIdAsync(userId);
            
            // Check if reaction already exists
            var existingReaction = await chatRepository.GetReactionAsync(messageId, userId);
            
            if (existingReaction != null)
            {
                // Update existing reaction
                existingReaction.ReactionType = reactionType.ToLower();
                existingReaction.CreatedAt = DateTime.UtcNow;
                await chatRepository.UpdateReactionAsync(existingReaction);
            }
            else
            {
                // Add new reaction
                var reaction = new SocialTechsy.SocialNetwork.Domain.Entities.MessageReaction
                {
                    MessageId = messageId,
                    UserId = userId,
                    ReactionType = reactionType.ToLower(),
                    CreatedAt = DateTime.UtcNow
                };
                await chatRepository.AddReactionAsync(reaction);
            }

            // Broadcast to all participants in the conversation
            var reactionDto = new
            {
                messageId,
                userId,
                username = user?.Username ?? "Unknown",
                profilePicture = user?.ProfilePicture,
                reactionType = reactionType.ToLower(),
                createdAt = DateTime.UtcNow.ToString("o")
            };

            // Get conversation to find participants
            var message = await chatRepository.GetMessageByIdAsync(messageId);
            if (message != null)
            {
                var conversation = await chatRepository.GetConversationByIdAsync(message.ConversationId);
                if (conversation != null)
                {
                    await Task.WhenAll(conversation.Participants.Select(p =>
                        Clients.Group($"user_{p.UserId}").SendAsync("ReceiveReaction", reactionDto)));
                }
            }

            _logger.LogInformation("User {UserId} added {ReactionType} reaction to message {MessageId}", 
                userId, reactionType, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reaction to message {MessageId}", messageId);
        }
    }

    /// <summary>
    /// Remove a reaction from a message
    /// </summary>
    public async Task RemoveReaction(int conversationId, int messageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();

            var reaction = await chatRepository.GetReactionAsync(messageId, userId);
            if (reaction == null) return;

            await chatRepository.RemoveReactionAsync(reaction);

            // Broadcast to all participants
            var message = await chatRepository.GetMessageByIdAsync(messageId);
            if (message != null)
            {
                var conversation = await chatRepository.GetConversationByIdAsync(message.ConversationId);
                if (conversation != null)
                {
                    await Task.WhenAll(conversation.Participants.Select(p =>
                        Clients.Group($"user_{p.UserId}").SendAsync("RemoveReaction", new { messageId, userId })));
                }
            }

            _logger.LogInformation("User {UserId} removed reaction from message {MessageId}", userId, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing reaction from message {MessageId}", messageId);
        }
    }
}
