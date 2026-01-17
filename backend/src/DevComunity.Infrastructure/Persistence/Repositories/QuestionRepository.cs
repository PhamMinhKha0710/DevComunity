using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Question entity
/// </summary>
public class QuestionRepository : IQuestionRepository
{
    private readonly DevComunityDbContext _context;

    public QuestionRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Include(q => q.User)
            .Include(q => q.Answers)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Comments)
            .FirstOrDefaultAsync(q => q.QuestionId == id, cancellationToken);
    }

    public async Task<IEnumerable<Question>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Include(q => q.User)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Question> Items, int TotalCount)> GetPaginatedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string? tag = null,
        string sort = "newest",
        CancellationToken cancellationToken = default)
    {
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.Answers)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .AsQueryable();

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(q => 
                q.Title.Contains(searchTerm) || 
                q.Body.Contains(searchTerm));
        }

        // Filter by tag
        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(q => 
                q.QuestionTags.Any(qt => qt.Tag.TagName == tag));
        }

        // Sort
        query = sort switch
        {
            "active" => query.OrderByDescending(q => q.UpdatedDate ?? q.CreatedDate),
            "votes" => query.OrderByDescending(q => q.Score),
            "unanswered" => query.Where(q => !q.Answers.Any()).OrderByDescending(q => q.CreatedDate),
            _ => query.OrderByDescending(q => q.CreatedDate) // newest
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default)
    {
        await _context.Questions.AddAsync(question, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return question;
    }

    public async Task UpdateAsync(Question question, CancellationToken cancellationToken = default)
    {
        _context.Questions.Update(question);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var question = await _context.Questions.FindAsync(new object[] { id }, cancellationToken);
        if (question != null)
        {
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Questions.AnyAsync(q => q.QuestionId == id, cancellationToken);
    }
}
