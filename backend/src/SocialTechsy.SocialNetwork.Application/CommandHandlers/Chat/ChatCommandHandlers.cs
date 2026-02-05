using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;

/// <summary>
/// Command for starting a conversation
/// </summary>
public class StartConversationCommand
{
    public int InitiatorId { get; set; }
    public int RecipientId { get; set; }
    public string? InitialMessage { get; set; }
}

/// <summary>
/// Handler for starting a conversation
/// </summary>
public class StartConversationCommandHandler
{
    private readonly IChatRepository _chatRepository;

    public StartConversationCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<ConversationDto> HandleAsync(StartConversationCommand command, CancellationToken cancellationToken)
    {
        // Check if conversation already exists between users
        var existing = await _chatRepository.GetConversationBetweenUsersAsync(
            command.InitiatorId, command.RecipientId, cancellationToken);

        if (existing != null)
        {
            // If initial message provided, add it
            if (!string.IsNullOrEmpty(command.InitialMessage))
            {
                var message = new Message
                {
                    ConversationId = existing.ConversationId,
                    SenderId = command.InitiatorId,
                    Content = command.InitialMessage,
                    SentDate = DateTime.UtcNow,
                    IsRead = false
                };
                await _chatRepository.AddMessageAsync(message, cancellationToken);
            }

            return new ConversationDto
            {
                ConversationId = existing.ConversationId,
                CreatedDate = existing.CreatedDate,
                LastMessageDate = existing.LastMessageDate
            };
        }

        // Create new conversation
        var conversation = new Conversation
        {
            IsGroupChat = false,
            CreatedDate = DateTime.UtcNow,
            LastMessageDate = !string.IsNullOrEmpty(command.InitialMessage) ? DateTime.UtcNow : null,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = command.InitiatorId, JoinedDate = DateTime.UtcNow },
                new() { UserId = command.RecipientId, JoinedDate = DateTime.UtcNow }
            }
        };

        var created = await _chatRepository.CreateConversationAsync(conversation, cancellationToken);

        // Add initial message if provided
        if (!string.IsNullOrEmpty(command.InitialMessage))
        {
            var message = new Message
            {
                ConversationId = created.ConversationId,
                SenderId = command.InitiatorId,
                Content = command.InitialMessage,
                SentDate = DateTime.UtcNow,
                IsRead = false
            };
            await _chatRepository.AddMessageAsync(message, cancellationToken);
        }

        return new ConversationDto
        {
            ConversationId = created.ConversationId,
            CreatedDate = created.CreatedDate,
            LastMessageDate = created.LastMessageDate
        };
    }
}

/// <summary>
/// Command for sending a message
/// </summary>
public class SendMessageCommand
{
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = null!;
    
    /// <summary>
    /// Optional: ID of message being replied to (for Reply/Quote feature)
    /// </summary>
    public int? ReplyToMessageId { get; set; }
}

/// <summary>
/// Handler for sending a message
/// </summary>
public class SendMessageCommandHandler
{
    private readonly IChatRepository _chatRepository;

    public SendMessageCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<MessageDto?> HandleAsync(SendMessageCommand command, CancellationToken cancellationToken)
    {
        // Verify user is part of conversation
        var conversation = await _chatRepository.GetConversationByIdAsync(command.ConversationId, cancellationToken);
        if (conversation == null || !conversation.Participants.Any(p => p.UserId == command.SenderId))
            return null;

        var message = new Message
        {
            ConversationId = command.ConversationId,
            SenderId = command.SenderId,
            Content = command.Content,
            SentDate = DateTime.UtcNow,
            IsRead = false,
            ReplyToMessageId = command.ReplyToMessageId
        };

        var created = await _chatRepository.AddMessageAsync(message, cancellationToken);

        // Get reply message info if applicable
        ReplyToMessageDto? replyToDto = null;
        if (command.ReplyToMessageId.HasValue)
        {
            var replyToMessage = await _chatRepository.GetMessageByIdAsync(command.ReplyToMessageId.Value, cancellationToken);
            if (replyToMessage != null)
            {
                replyToDto = new ReplyToMessageDto
                {
                    MessageId = replyToMessage.MessageId,
                    SenderId = replyToMessage.SenderId,
                    SenderUsername = replyToMessage.Sender?.Username ?? "",
                    Content = replyToMessage.Content.Length > 100 
                        ? replyToMessage.Content.Substring(0, 100) + "..." 
                        : replyToMessage.Content
                };
            }
        }

        return new MessageDto
        {
            MessageId = created.MessageId,
            ConversationId = created.ConversationId,
            SenderId = created.SenderId,
            Content = created.Content,
            SentDate = created.SentDate,
            IsRead = created.IsRead,
            ReplyToMessageId = created.ReplyToMessageId,
            ReplyToMessage = replyToDto
        };
    }
}

/// <summary>
/// Command for marking messages as read
/// </summary>
public class MarkConversationReadCommand
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Handler for marking messages as read
/// </summary>
public class MarkConversationReadCommandHandler
{
    private readonly IChatRepository _chatRepository;

    public MarkConversationReadCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task HandleAsync(MarkConversationReadCommand command, CancellationToken cancellationToken)
    {
        await _chatRepository.MarkMessagesAsReadAsync(command.ConversationId, command.UserId, cancellationToken);
    }
}

/// <summary>
/// Command for adding a reaction to a message
/// </summary>
public class AddReactionCommand
{
    public int MessageId { get; set; }
    public int UserId { get; set; }
    public string ReactionType { get; set; } = null!;
}

/// <summary>
/// Handler for adding a reaction to a message
/// </summary>
public class AddReactionCommandHandler
{
    private readonly IChatRepository _chatRepository;

    public AddReactionCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<MessageReactionDto?> HandleAsync(AddReactionCommand command, CancellationToken cancellationToken)
    {
        // Validate reaction type
        var validReactions = new[] { "like", "love", "haha", "wow", "sad", "angry" };
        if (!validReactions.Contains(command.ReactionType.ToLower()))
            return null;

        // Check if message exists
        var message = await _chatRepository.GetMessageByIdAsync(command.MessageId, cancellationToken);
        if (message == null)
            return null;

        // Check if user already has a reaction on this message
        var existingReaction = await _chatRepository.GetReactionAsync(command.MessageId, command.UserId, cancellationToken);
        
        if (existingReaction != null)
        {
            // Update existing reaction
            existingReaction.ReactionType = command.ReactionType.ToLower();
            existingReaction.CreatedAt = DateTime.UtcNow;
            await _chatRepository.UpdateReactionAsync(existingReaction, cancellationToken);
            
            return new MessageReactionDto
            {
                MessageReactionId = existingReaction.MessageReactionId,
                UserId = existingReaction.UserId,
                ReactionType = existingReaction.ReactionType,
                CreatedAt = existingReaction.CreatedAt
            };
        }

        // Add new reaction
        var reaction = new MessageReaction
        {
            MessageId = command.MessageId,
            UserId = command.UserId,
            ReactionType = command.ReactionType.ToLower(),
            CreatedAt = DateTime.UtcNow
        };

        var created = await _chatRepository.AddReactionAsync(reaction, cancellationToken);

        return new MessageReactionDto
        {
            MessageReactionId = created.MessageReactionId,
            UserId = created.UserId,
            ReactionType = created.ReactionType,
            CreatedAt = created.CreatedAt
        };
    }
}

/// <summary>
/// Command for removing a reaction from a message
/// </summary>
public class RemoveReactionCommand
{
    public int MessageId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Handler for removing a reaction from a message
/// </summary>
public class RemoveReactionCommandHandler
{
    private readonly IChatRepository _chatRepository;

    public RemoveReactionCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<bool> HandleAsync(RemoveReactionCommand command, CancellationToken cancellationToken)
    {
        var reaction = await _chatRepository.GetReactionAsync(command.MessageId, command.UserId, cancellationToken);
        if (reaction == null)
            return false;

        await _chatRepository.RemoveReactionAsync(reaction, cancellationToken);
        return true;
    }
}