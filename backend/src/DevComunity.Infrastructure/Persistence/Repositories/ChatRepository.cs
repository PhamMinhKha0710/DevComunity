using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Chat entities (Conversation, Message)
/// </summary>
public class ChatRepository : IChatRepository
{
    private readonly DevComunityDbContext _context;

    public ChatRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetConversationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.ConversationId == id, cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.LastMessageDate ?? c.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation?> GetConversationBetweenUsersAsync(int userId1, int userId2, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .Where(c => c.Participants.Count == 2 &&
                        c.Participants.Any(p => p.UserId == userId1) &&
                        c.Participants.Any(p => p.UserId == userId2))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Conversation> CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _context.Conversations.AddAsync(conversation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetMessagesAsync(
        int conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Message> AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
        
        // Update conversation's last message time
        var conversation = await _context.Conversations.FindAsync(new object[] { message.ConversationId }, cancellationToken);
        if (conversation != null)
        {
            conversation.LastMessageDate = message.SentDate;
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task MarkMessagesAsReadAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), cancellationToken);
    }
}

