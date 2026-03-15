using System.Text.Json;
using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;
using MessageType = SocialTechsy.SocialNetwork.Domain.Enums.MessageType;
using ReactionType = SocialTechsy.SocialNetwork.Domain.Enums.ReactionType;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;

#region Result Types

public class SendMessageResult
{
    public MessageDto Message { get; set; } = null!;
    public List<int> OtherParticipantUserIds { get; set; } = new();
    public string? SenderDisplayName { get; set; }
    public string NotificationPreview { get; set; } = null!;
    public string GroupTier { get; set; } = "small";
}

#endregion

#region Start Conversation

public class StartConversationCommand : IRequest<ConversationDto>
{
    public int InitiatorId { get; set; }
    public int RecipientId { get; set; }
    public string? InitialMessage { get; set; }
}

public class StartConversationCommandHandler : IRequestHandler<StartConversationCommand, ConversationDto>
{
    private readonly IChatRepository _chatRepository;

    public StartConversationCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<ConversationDto> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _chatRepository.GetConversationBetweenUsersAsync(
            request.InitiatorId, request.RecipientId, cancellationToken);

        if (existing != null)
        {
            if (!string.IsNullOrEmpty(request.InitialMessage))
            {
                var message = new Message
                {
                    ConversationId = existing.ConversationId,
                    SenderId = request.InitiatorId,
                    Content = request.InitialMessage,
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

        var conversation = new Conversation
        {
            IsGroupChat = false,
            CreatedDate = DateTime.UtcNow,
            LastMessageDate = !string.IsNullOrEmpty(request.InitialMessage) ? DateTime.UtcNow : null,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = request.InitiatorId, JoinedDate = DateTime.UtcNow },
                new() { UserId = request.RecipientId, JoinedDate = DateTime.UtcNow }
            }
        };

        var created = await _chatRepository.CreateConversationAsync(conversation, cancellationToken);

        if (!string.IsNullOrEmpty(request.InitialMessage))
        {
            var message = new Message
            {
                ConversationId = created.ConversationId,
                SenderId = request.InitiatorId,
                Content = request.InitialMessage,
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

#endregion

#region Log Call Event

public class LogCallEventCommand : IRequest<bool>
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public string CallEventType { get; set; } = null!; // ended, rejected, missed, cancelled
    public string CallType { get; set; } = "audio"; // audio, video
    public int? DurationSeconds { get; set; }
}

public class LogCallEventCommandHandler : IRequestHandler<LogCallEventCommand, bool>
{
    private readonly IChatRepository _chatRepository;

    public LogCallEventCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<bool> Handle(LogCallEventCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _chatRepository.GetConversationByIdAsync(request.ConversationId, cancellationToken);
        if (conversation == null || !conversation.Participants.Any(p => p.UserId == request.UserId))
            return false;

        var validEvents = new[] { "ended", "rejected", "missed", "cancelled" };
        if (!validEvents.Contains(request.CallEventType, StringComparer.OrdinalIgnoreCase))
            return false;

        var message = Message.CreateCallEvent(
            request.ConversationId,
            request.UserId,
            request.CallEventType.ToLowerInvariant(),
            request.CallType.ToLowerInvariant(),
            request.DurationSeconds);

        await _chatRepository.AddMessageAsync(message, cancellationToken);
        return true;
    }
}

#endregion

#region Send Message (text + media unified)

public class SendMessageCommand : IRequest<SendMessageResult?>
{
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = null!;
    public long? ReplyToMessageId { get; set; }
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public long? AttachmentSize { get; set; }
}

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SendMessageResult?>
{
    private readonly IChatRepository _chatRepository;
    private readonly IUserRepository _userRepository;

    public SendMessageCommandHandler(
        IChatRepository chatRepository,
        IUserRepository userRepository)
    {
        _chatRepository = chatRepository;
        _userRepository = userRepository;
    }

    public async Task<SendMessageResult?> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _chatRepository.GetConversationByIdAsync(request.ConversationId, cancellationToken);
        if (conversation == null || !conversation.Participants.Any(p => p.UserId == request.SenderId))
            return null;

        var sender = await _userRepository.GetByIdAsync(request.SenderId);

        var messageTypeString = request.MessageType.ToString().ToLowerInvariant();

        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderId = request.SenderId,
            Content = request.Content,
            MessageType = messageTypeString,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentFileName = request.AttachmentFileName,
            AttachmentSize = request.AttachmentSize ?? 0,
            SentDate = DateTime.UtcNow,
            IsRead = false,
            ReplyToMessageId = request.ReplyToMessageId
        };

        // AddMessageAsync now writes both the message and an outbox event atomically
        var saved = await _chatRepository.AddMessageAsync(message, cancellationToken);

        ReplyToMessageDto? replyToDto = null;
        if (request.ReplyToMessageId.HasValue)
        {
            var replyTo = await _chatRepository.GetMessageByIdAsync(request.ReplyToMessageId.Value, cancellationToken);
            if (replyTo != null)
            {
                replyToDto = new ReplyToMessageDto
                {
                    MessageId = replyTo.MessageId,
                    SenderId = replyTo.SenderId,
                    SenderUsername = replyTo.Sender?.Username ?? "",
                    Content = replyTo.Content.Length > 100
                        ? replyTo.Content[..100] + "..."
                        : replyTo.Content
                };
            }
        }

        var senderUsername = sender?.Username ?? "Unknown";
        var senderDisplayName = sender?.DisplayName ?? senderUsername;

        var messageDto = new MessageDto
        {
            MessageId = saved.MessageId,
            ConversationId = saved.ConversationId,
            SenderId = saved.SenderId,
            SenderUsername = senderUsername,
            SenderProfilePicture = sender?.ProfilePicture,
            Content = saved.Content,
            MessageType = saved.MessageType ?? "text",
            AttachmentUrl = saved.AttachmentUrl,
            AttachmentFileName = saved.AttachmentFileName,
            AttachmentSize = saved.AttachmentSize,
            SentDate = saved.SentDate,
            IsRead = saved.IsRead,
            ReplyToMessageId = saved.ReplyToMessageId,
            ReplyToMessage = replyToDto
        };

        string notificationPreview;
        if (request.MessageType != MessageType.Text)
        {
            notificationPreview = request.MessageType switch
            {
                MessageType.Image => "Photo",
                MessageType.Video => "Video",
                MessageType.Audio => "Audio",
                _ => request.AttachmentFileName ?? "File"
            };
        }
        else
        {
            notificationPreview = request.Content.Length > 50 ? request.Content[..50] + "..." : request.Content;
        }

        var participantIds = conversation.Participants.Select(p => p.UserId).ToList();
        var groupTier = participantIds.Count switch
        {
            <= 50 => "small",
            <= 500 => "medium",
            _ => "large"
        };

        return new SendMessageResult
        {
            Message = messageDto,
            OtherParticipantUserIds = participantIds.Where(id => id != request.SenderId).ToList(),
            SenderDisplayName = senderDisplayName,
            NotificationPreview = notificationPreview,
            GroupTier = groupTier
        };
    }
}

#endregion

#region Mark Conversation Read

public class MarkConversationReadCommand : IRequest
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public int LastReadMessageId { get; set; }
}

public class MarkConversationReadCommandHandler : IRequestHandler<MarkConversationReadCommand>
{
    private readonly IChatRepository _chatRepository;
    private readonly IChatMessageBroker? _broker;

    public MarkConversationReadCommandHandler(IChatRepository chatRepository, IChatMessageBroker? broker = null)
    {
        _chatRepository = chatRepository;
        _broker = broker;
    }

    public async Task Handle(MarkConversationReadCommand request, CancellationToken cancellationToken)
    {
        if (request.LastReadMessageId > 0)
        {
            await _chatRepository.UpdateReadWatermarkAsync(
                request.ConversationId, request.UserId, request.LastReadMessageId, cancellationToken);
        }
        else
        {
            await _chatRepository.MarkMessagesAsReadAsync(request.ConversationId, request.UserId, cancellationToken);
        }

        if (_broker == null) return;
        try
        {
            await _broker.PublishAsync(new ChatEvent
            {
                Type = ChatEventTypes.MessagesRead,
                PayloadJson = JsonSerializer.Serialize(new MessagesReadPayload
                {
                    ConversationId = request.ConversationId,
                    UserId = request.UserId
                }),
                Timestamp = DateTime.UtcNow
            });
        }
        catch { /* non-critical */ }
    }
}

#endregion

#region Leave Conversation

public class LeaveConversationCommand : IRequest<bool>
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
}

public class LeaveConversationCommandHandler : IRequestHandler<LeaveConversationCommand, bool>
{
    private readonly IChatRepository _chatRepository;

    public LeaveConversationCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<bool> Handle(LeaveConversationCommand request, CancellationToken cancellationToken)
    {
        return await _chatRepository.RemoveParticipantAsync(request.ConversationId, request.UserId, cancellationToken);
    }
}

#endregion

#region Add Reaction

public class AddReactionCommand : IRequest<MessageReactionDto?>
{
    public long MessageId { get; set; }
    public int UserId { get; set; }
    public ReactionType ReactionType { get; set; }
}

public class AddReactionCommandHandler : IRequestHandler<AddReactionCommand, MessageReactionDto?>
{
    private readonly IChatRepository _chatRepository;
    private readonly IUserRepository _userRepository;

    public AddReactionCommandHandler(IChatRepository chatRepository, IUserRepository userRepository)
    {
        _chatRepository = chatRepository;
        _userRepository = userRepository;
    }

    public async Task<MessageReactionDto?> Handle(AddReactionCommand request, CancellationToken cancellationToken)
    {
        var reactionTypeString = request.ReactionType.ToString().ToLowerInvariant();

        var message = await _chatRepository.GetMessageByIdAsync(request.MessageId, cancellationToken);
        if (message == null)
            return null;

        var user = await _userRepository.GetByIdAsync(request.UserId);

        var existingReaction = await _chatRepository.GetReactionAsync(request.MessageId, request.UserId, cancellationToken);

        if (existingReaction != null)
        {
            existingReaction.ReactionType = reactionTypeString;
            existingReaction.CreatedAt = DateTime.UtcNow;
            await _chatRepository.UpdateReactionAsync(existingReaction, cancellationToken);

            return new MessageReactionDto
            {
                MessageReactionId = existingReaction.MessageReactionId,
                UserId = existingReaction.UserId,
                Username = user?.Username ?? "Unknown",
                ProfilePicture = user?.ProfilePicture,
                ReactionType = existingReaction.ReactionType,
                CreatedAt = existingReaction.CreatedAt
            };
        }

        var reaction = new MessageReaction
        {
            MessageId = request.MessageId,
            UserId = request.UserId,
            ReactionType = reactionTypeString,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _chatRepository.AddReactionAsync(reaction, cancellationToken);

        return new MessageReactionDto
        {
            MessageReactionId = created.MessageReactionId,
            UserId = created.UserId,
            Username = user?.Username ?? "Unknown",
            ProfilePicture = user?.ProfilePicture,
            ReactionType = created.ReactionType,
            CreatedAt = created.CreatedAt
        };
    }
}

#endregion

#region Acknowledge Delivery

public class AcknowledgeDeliveryCommand : IRequest<bool>
{
    public long MessageId { get; set; }
    public int UserId { get; set; }
    public DeliveryStatus Status { get; set; }
}

public class AcknowledgeDeliveryCommandHandler : IRequestHandler<AcknowledgeDeliveryCommand, bool>
{
    private readonly IChatRepository _chatRepository;

    public AcknowledgeDeliveryCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<bool> Handle(AcknowledgeDeliveryCommand request, CancellationToken cancellationToken)
    {
        var message = await _chatRepository.GetMessageByIdAsync(request.MessageId, cancellationToken);
        if (message == null) return false;

        if ((int)request.Status <= (int)message.DeliveryStatus)
            return false;

        await _chatRepository.UpdateDeliveryStatusAsync(request.MessageId, request.Status, cancellationToken);
        return true;
    }
}

#endregion

#region Remove Reaction

public class RemoveReactionCommand : IRequest<bool>
{
    public long MessageId { get; set; }
    public int UserId { get; set; }
}

public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand, bool>
{
    private readonly IChatRepository _chatRepository;

    public RemoveReactionCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<bool> Handle(RemoveReactionCommand request, CancellationToken cancellationToken)
    {
        var reaction = await _chatRepository.GetReactionAsync(request.MessageId, request.UserId, cancellationToken);
        if (reaction == null)
            return false;

        await _chatRepository.RemoveReactionAsync(reaction, cancellationToken);
        return true;
    }
}

#endregion
