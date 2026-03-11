using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Chat entities (Conversation, Message, MessageReaction)
/// </summary>
public interface IChatRepository
{
    // Conversations
    Task<Conversation?> GetConversationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Conversation> Items, int TotalCount)> GetUserConversationsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationBetweenUsersAsync(int userId1, int userId2, CancellationToken cancellationToken = default);
    Task<Conversation> CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
    
    // Messages
    Task<Message?> GetMessageByIdAsync(int messageId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Message> Items, int TotalCount)> GetMessagesAsync(
        int conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetMessagesCursorAsync(
        int conversationId,
        int? afterMessageId,
        int limit,
        CancellationToken cancellationToken = default);
    Task<Message> AddMessageAsync(Message message, CancellationToken cancellationToken = default);
    Task MarkMessagesAsReadAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
    Task UpdateDeliveryStatusAsync(int messageId, DeliveryStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetMessagesSinceAsync(int conversationId, int sinceMessageId, int limit = 200, CancellationToken cancellationToken = default);
    
    // Reactions
    Task<MessageReaction?> GetReactionAsync(int messageId, int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MessageReaction>> GetMessageReactionsAsync(int messageId, CancellationToken cancellationToken = default);
    Task<MessageReaction> AddReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default);
    Task UpdateReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default);
    Task RemoveReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default);
}
