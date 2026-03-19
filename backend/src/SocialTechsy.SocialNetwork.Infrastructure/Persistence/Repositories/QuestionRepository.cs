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
    private readonly bool _fullTextEnabled;

    public QuestionRepository(
        SocialTechsySocialNetworkDbContext context,
        bool fullTextEnabled = false)
    {
        _context = context;
        _fullTextEnabled = fullTextEnabled;
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

    public async Task<Question?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        // For update operations, we only need the Question entity itself.
        // This avoids EF Core tracking the entire graph (User, Answers, QuestionTags, Comments),
        // which would cause conflicts when updating question and tags in the same request.
        return await _context.Questions
            .FirstOrDefaultAsync(q => q.QuestionId == id, cancellationToken);
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

        query = ApplyFilters(query, searchTerm, tag, sort);

        // Execute sequentially - DbContext cannot be used concurrently
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Optimized paginated query with minimal includes for better performance.
    /// Does NOT include Answers - just Author and Tags.
    /// </summary>
    public async Task<(IEnumerable<Question> Items, int TotalCount)> GetPaginatedOptimizedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string? tag = null,
        string sort = "newest",
        CancellationToken cancellationToken = default)
    {
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .AsQueryable();

        query = ApplyFilters(query, searchTerm, tag, sort);

        // Execute sequentially - DbContext cannot be used concurrently
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<Question> ApplyFilters(
        IQueryable<Question> query,
        string? searchTerm,
        string? tag,
        string sort)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            if (_fullTextEnabled)
            {
                query = query.Where(q =>
                    EF.Functions.FreeText(q.Title, term) ||
                    EF.Functions.FreeText(q.Body, term));
            }
            else
            {
                query = query.Where(q =>
                    q.Title.Contains(term) ||
                    q.Body.Contains(term));
            }
        }

        // Filter by tag
        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(q =>
                q.QuestionTags.Any(qt => qt.Tag.TagName == tag));
        }

        // Sort
        return sort switch
        {
            "active" => query.OrderByDescending(q => q.UpdatedDate ?? q.CreatedDate),
            "votes" => query.OrderByDescending(q => q.Score),
            "unanswered" => query.Where(q => !q.Answers.Any()).OrderByDescending(q => q.CreatedDate),
            _ => query.OrderByDescending(q => q.CreatedDate)
        };
    }

    public async Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default)
    {
        await _context.Questions.AddAsync(question, cancellationToken);
        return question;
    }

    public async Task UpdateAsync(Question question, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(question);
        if (entry.State == EntityState.Detached)
        {
            _context.Questions.Update(question);
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var question = await _context.Questions.FindAsync(new object[] { id }, cancellationToken);
        if (question != null)
        {
            _context.Questions.Remove(question);
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

    public async Task IncrementViewCountByDeltaAsync(int id, long delta, CancellationToken cancellationToken = default)
    {
        if (delta <= 0) return;
        await _context.Questions
            .Where(q => q.QuestionId == id)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(q => q.ViewCount, q => q.ViewCount + (int)delta), cancellationToken);
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
