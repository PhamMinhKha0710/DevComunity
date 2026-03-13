using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Chat entities (Conversation, Message)
/// </summary>
public class ChatRepository : IChatRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public ChatRepository(SocialTechsySocialNetworkDbContext context)
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

    public async Task<(IEnumerable<Conversation> Items, int TotalCount)> GetUserConversationsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentDate).Take(1))
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.LastMessageDate ?? c.CreatedDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
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

    public async Task<Message?> GetMessageByIdAsync(int messageId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
            .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetMessagesAsync(
        int conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
            .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<IEnumerable<Message>> GetMessagesCursorAsync(
        int conversationId, int? afterMessageId, int limit, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Cursor pagination only supported with MongoDB");
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

    public async Task<bool> RemoveParticipantAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, cancellationToken);

        if (participant == null)
        {
            return false;
        }

        _context.ConversationParticipants.Remove(participant);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkMessagesAsReadAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), cancellationToken);
    }

    public async Task UpdateReadWatermarkAsync(int conversationId, int userId, int lastReadMessageId, CancellationToken cancellationToken = default)
    {
        await MarkMessagesAsReadAsync(conversationId, userId, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .CountAsync(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead, cancellationToken);
    }

    public async Task UpdateDeliveryStatusAsync(int messageId, DeliveryStatus status, CancellationToken cancellationToken = default)
    {
        await _context.Messages
            .Where(m => m.MessageId == messageId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.DeliveryStatus, status), cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetMessagesSinceAsync(int conversationId, int sinceMessageId, int limit = 200, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Sender)
            .Where(m => m.ConversationId == conversationId && m.MessageId > sinceMessageId)
            .OrderBy(m => m.MessageId)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // ========== REACTIONS ==========

    public async Task<MessageReaction?> GetReactionAsync(int messageId, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.MessageReactions
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<MessageReaction>> GetMessageReactionsAsync(int messageId, CancellationToken cancellationToken = default)
    {
        return await _context.MessageReactions
            .Include(r => r.User)
            .Where(r => r.MessageId == messageId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MessageReaction> AddReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default)
    {
        await _context.MessageReactions.AddAsync(reaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return reaction;
    }

    public async Task UpdateReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default)
    {
        _context.MessageReactions.Update(reaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default)
    {
        _context.MessageReactions.Remove(reaction);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

