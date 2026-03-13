using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Chat;

public class GetConversationsQuery : IRequest<PaginatedResponse<ConversationDto>>
{
    public int UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetConversationByIdQuery : IRequest<ConversationDto?>
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
}

public class GetMessagesQuery : IRequest<PaginatedResponse<MessageDto>>
{
    public int ConversationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Handler for getting user's conversations
/// </summary>
public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, PaginatedResponse<ConversationDto>>
{
    private readonly IChatRepository _chatRepository;

    public GetConversationsQueryHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<PaginatedResponse<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var (conversations, totalCount) = await _chatRepository.GetUserConversationsAsync(request.UserId, request.Page, request.PageSize, cancellationToken);

        return new PaginatedResponse<ConversationDto>
        {
            Items = conversations.Select(c =>
            {
                var lastMsg = c.Messages?.OrderByDescending(m => m.SentDate).FirstOrDefault();
                var preview = lastMsg?.Content;
                if (preview != null && preview.Length > 50)
                    preview = preview.Substring(0, 50) + "...";
                
                return new ConversationDto
                {
                    ConversationId = c.ConversationId,
                    Title = c.Title,
                    IsGroupChat = c.IsGroupChat,
                    CreatedDate = c.CreatedDate,
                    LastMessageDate = c.LastMessageDate,
                    LastMessagePreview = preview,
                    UnreadCount = c.Messages?.Count(m => !m.IsRead && m.SenderId != request.UserId) ?? 0,
                    Participants = c.Participants.Select(p => new ConversationParticipantDto
                    {
                        UserId = p.User?.UserId ?? 0,
                        Username = p.User?.Username ?? "",
                        DisplayName = p.User?.DisplayName,
                        ProfilePicture = p.User?.ProfilePicture
                    }).ToList()
                };
            }).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting conversation by ID
/// </summary>
public class GetConversationByIdQueryHandler : IRequestHandler<GetConversationByIdQuery, ConversationDto?>
{
    private readonly IChatRepository _chatRepository;

    public GetConversationByIdQueryHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<ConversationDto?> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _chatRepository.GetConversationByIdAsync(request.ConversationId, cancellationToken);
        if (c == null) return null;

        if (!c.Participants.Any(p => p.UserId == request.UserId))
            return null;

        return new ConversationDto
        {
            ConversationId = c.ConversationId,
            Title = c.Title,
            IsGroupChat = c.IsGroupChat,
            CreatedDate = c.CreatedDate,
            LastMessageDate = c.LastMessageDate,
            Participants = c.Participants.Select(p => new ConversationParticipantDto
            {
                UserId = p.User?.UserId ?? 0,
                Username = p.User?.Username ?? "",
                DisplayName = p.User?.DisplayName,
                ProfilePicture = p.User?.ProfilePicture
            }).ToList()
        };
    }
}

/// <summary>
/// Handler for getting messages in a conversation
/// </summary>
public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, PaginatedResponse<MessageDto>>
{
    private readonly IChatRepository _chatRepository;

    public GetMessagesQueryHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<PaginatedResponse<MessageDto>> Handle(
        GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _chatRepository.GetMessagesAsync(request.ConversationId, request.Page, request.PageSize, cancellationToken);

        return new PaginatedResponse<MessageDto>
        {
            Items = items.Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderUsername = m.Sender?.Username ?? "",
                SenderProfilePicture = m.Sender?.ProfilePicture,
                Content = m.Content,
                SentDate = m.SentDate,
                IsRead = m.IsRead,
                MessageType = m.MessageType,
                AttachmentUrl = m.AttachmentUrl,
                AttachmentFileName = m.AttachmentFileName,
                AttachmentSize = m.AttachmentSize,
                ReplyToMessageId = m.ReplyToMessageId,
                ReplyToMessage = m.ReplyToMessage != null ? new ReplyToMessageDto
                {
                    MessageId = m.ReplyToMessage.MessageId,
                    SenderId = m.ReplyToMessage.SenderId,
                    SenderUsername = m.ReplyToMessage.Sender?.Username ?? "",
                    Content = m.ReplyToMessage.Content.Length > 100 
                        ? m.ReplyToMessage.Content.Substring(0, 100) + "..." 
                        : m.ReplyToMessage.Content
                } : null,
                Reactions = m.Reactions?.Select(r => new MessageReactionDto
                {
                    MessageReactionId = r.MessageReactionId,
                    UserId = r.UserId,
                    Username = r.User?.Username ?? "",
                    ProfilePicture = r.User?.ProfilePicture,
                    ReactionType = r.ReactionType,
                    CreatedAt = r.CreatedAt
                }).ToList() ?? new List<MessageReactionDto>()
            }).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

public class GetMessagesSinceQuery : IRequest<List<MessageDto>>
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public long SinceMessageId { get; set; }
    public int Limit { get; set; } = 200;
}

public class GetMessagesSinceQueryHandler : IRequestHandler<GetMessagesSinceQuery, List<MessageDto>>
{
    private readonly IChatRepository _chatRepository;

    public GetMessagesSinceQueryHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<List<MessageDto>> Handle(GetMessagesSinceQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _chatRepository.GetConversationByIdAsync(request.ConversationId, cancellationToken);
        if (conversation == null || !conversation.Participants.Any(p => p.UserId == request.UserId))
            return new List<MessageDto>();

        var messages = await _chatRepository.GetMessagesSinceAsync(
            request.ConversationId, request.SinceMessageId, request.Limit, cancellationToken);

        return messages.Select(m => new MessageDto
        {
            MessageId = m.MessageId,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderUsername = m.Sender?.Username ?? "",
            SenderProfilePicture = m.Sender?.ProfilePicture,
            Content = m.Content,
            SentDate = m.SentDate,
            IsRead = m.IsRead,
            MessageType = m.MessageType,
            DeliveryStatus = m.DeliveryStatus.ToString().ToLower(),
            AttachmentUrl = m.AttachmentUrl,
            AttachmentFileName = m.AttachmentFileName,
            AttachmentSize = m.AttachmentSize,
            ReplyToMessageId = m.ReplyToMessageId,
            ReplyToMessage = m.ReplyToMessage != null ? new ReplyToMessageDto
            {
                MessageId = m.ReplyToMessage.MessageId,
                SenderId = m.ReplyToMessage.SenderId,
                SenderUsername = m.ReplyToMessage.Sender?.Username ?? "",
                Content = m.ReplyToMessage.Content.Length > 100
                    ? m.ReplyToMessage.Content[..100] + "..."
                    : m.ReplyToMessage.Content
            } : null,
            Reactions = m.Reactions?.Select(r => new MessageReactionDto
            {
                MessageReactionId = r.MessageReactionId,
                UserId = r.UserId,
                Username = r.User?.Username ?? "",
                ProfilePicture = r.User?.ProfilePicture,
                ReactionType = r.ReactionType,
                CreatedAt = r.CreatedAt
            }).ToList() ?? new List<MessageReactionDto>()
        }).ToList();
    }
}
