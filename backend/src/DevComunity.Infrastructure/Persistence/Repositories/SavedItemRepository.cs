using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for SavedItem entity
/// </summary>
public class SavedItemRepository : ISavedItemRepository
{
    private readonly DevComunityDbContext _context;

    public SavedItemRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<SavedItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.SavedItems
            .Include(s => s.Question)
            .Include(s => s.Answer)
            .FirstOrDefaultAsync(s => s.SavedItemId == id, cancellationToken);
    }

    public async Task<IEnumerable<SavedItem>> GetByUserIdAsync(int userId, string? type = null, int page = 1, int pageSize = 15, CancellationToken cancellationToken = default)
    {
        var query = _context.SavedItems
            .Include(s => s.Question)
                .ThenInclude(q => q!.User)
            .Include(s => s.Question)
                .ThenInclude(q => q!.Answers)
            .Include(s => s.Answer)
                .ThenInclude(a => a!.User)
            .Include(s => s.Answer)
                .ThenInclude(a => a!.Question)
            .Where(s => s.UserId == userId);

        if (type?.ToLower() == "question")
            query = query.Where(s => s.QuestionId != null);
        else if (type?.ToLower() == "answer")
            query = query.Where(s => s.AnswerId != null);

        return await query
            .OrderByDescending(s => s.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByUserIdAsync(int userId, string? type = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SavedItems.Where(s => s.UserId == userId);

        if (type?.ToLower() == "question")
            query = query.Where(s => s.QuestionId != null);
        else if (type?.ToLower() == "answer")
            query = query.Where(s => s.AnswerId != null);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<SavedItem?> FindAsync(int userId, int? questionId, int? answerId, CancellationToken cancellationToken = default)
    {
        return await _context.SavedItems
            .FirstOrDefaultAsync(s => 
                s.UserId == userId && 
                s.QuestionId == questionId && 
                s.AnswerId == answerId, 
                cancellationToken);
    }

    public async Task<bool> IsSavedAsync(int userId, int? questionId, int? answerId, CancellationToken cancellationToken = default)
    {
        return await _context.SavedItems
            .AnyAsync(s => 
                s.UserId == userId && 
                s.QuestionId == questionId && 
                s.AnswerId == answerId, 
                cancellationToken);
    }

    public async Task<SavedItem> AddAsync(SavedItem savedItem, CancellationToken cancellationToken = default)
    {
        await _context.SavedItems.AddAsync(savedItem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return savedItem;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _context.SavedItems.FindAsync(new object[] { id }, cancellationToken);
        if (item != null)
        {
            _context.SavedItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByQuestionAsync(int userId, int questionId, CancellationToken cancellationToken = default)
    {
        var item = await _context.SavedItems
            .FirstOrDefaultAsync(s => s.UserId == userId && s.QuestionId == questionId, cancellationToken);
        if (item != null)
        {
            _context.SavedItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByAnswerAsync(int userId, int answerId, CancellationToken cancellationToken = default)
    {
        var item = await _context.SavedItems
            .FirstOrDefaultAsync(s => s.UserId == userId && s.AnswerId == answerId, cancellationToken);
        if (item != null)
        {
            _context.SavedItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
