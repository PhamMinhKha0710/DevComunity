using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;

namespace DevComunity.Application.CommandHandlers.Chat;

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
            IsRead = false
        };

        var created = await _chatRepository.AddMessageAsync(message, cancellationToken);

        return new MessageDto
        {
            MessageId = created.MessageId,
            ConversationId = created.ConversationId,
            SenderId = created.SenderId,
            Content = created.Content,
            SentDate = created.SentDate,
            IsRead = created.IsRead
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
