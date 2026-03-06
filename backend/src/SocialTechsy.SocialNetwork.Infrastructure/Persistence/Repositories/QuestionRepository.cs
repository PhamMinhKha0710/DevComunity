using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Question entity
/// </summary>
public class QuestionRepository : IQuestionRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public QuestionRepository(SocialTechsySocialNetworkDbContext context)
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

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(q =>
                EF.Functions.Like(q.Title, $"%{term}%") ||
                EF.Functions.Like(q.Body, $"%{term}%"));
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

    public async Task IncrementViewCountAsync(int id, CancellationToken cancellationToken = default)
    {
        await _context.Questions
            .Where(q => q.QuestionId == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(q => q.ViewCount, q => q.ViewCount + 1), cancellationToken);
    }

    public async Task<(IEnumerable<Question> Items, int TotalCount)> GetByUserIdAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
