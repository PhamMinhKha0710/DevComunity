using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Chat;

/// <summary>
/// Handler for getting user's conversations
/// </summary>
public class GetConversationsQueryHandler
{
    private readonly IChatRepository _chatRepository;

    public GetConversationsQueryHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<IEnumerable<ConversationDto>> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        var conversations = await _chatRepository.GetUserConversationsAsync(userId, cancellationToken);

        return conversations.Select(c => new ConversationDto
        {
            ConversationId = c.ConversationId,
            Title = c.Title,
            IsGroupChat = c.IsGroupChat,
            CreatedDate = c.CreatedDate,
            LastMessageDate = c.LastMessageDate,
            LastMessagePreview = c.Messages.OrderByDescending(m => m.SentDate).FirstOrDefault()?.Content?.Substring(0, Math.Min(50, c.Messages.OrderByDescending(m => m.SentDate).FirstOrDefault()?.Content?.Length ?? 0)),
            UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != userId),
            Participants = c.Participants.Select(p => new ConversationParticipantDto
            {
                UserId = p.User?.UserId ?? 0,
                Username = p.User?.Username ?? "",
                DisplayName = p.User?.DisplayName,
                ProfilePicture = p.User?.ProfilePicture
            }).ToList()
        }).ToList();
    }
}

/// <summary>
/// Handler for getting conversation by ID
/// </summary>
public class GetConversationByIdQueryHandler
{
    private readonly IChatRepository _chatRepository;

    public GetConversationByIdQueryHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<ConversationDto?> HandleAsync(int conversationId, int userId, CancellationToken cancellationToken)
    {
        var c = await _chatRepository.GetConversationByIdAsync(conversationId, cancellationToken);
        if (c == null) return null;

        // Check if user is part of the conversation
        if (!c.Participants.Any(p => p.UserId == userId))
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
public class GetMessagesQueryHandler
{
    private readonly IChatRepository _chatRepository;

    public GetMessagesQueryHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<PaginatedResponse<MessageDto>> HandleAsync(
        int conversationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _chatRepository.GetMessagesAsync(conversationId, page, pageSize, cancellationToken);

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
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
