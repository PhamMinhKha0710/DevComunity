using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Chat entities (Conversation, Message)
/// </summary>
public interface IChatRepository
{
    // Conversations
    Task<Conversation?> GetConversationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationBetweenUsersAsync(int userId1, int userId2, CancellationToken cancellationToken = default);
    Task<Conversation> CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
    
    // Messages
    Task<(IEnumerable<Message> Items, int TotalCount)> GetMessagesAsync(
        int conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Message> AddMessageAsync(Message message, CancellationToken cancellationToken = default);
    Task MarkMessagesAsReadAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
}
