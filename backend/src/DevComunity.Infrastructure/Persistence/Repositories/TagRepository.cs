using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Tag entity
/// </summary>
public class TagRepository : ITagRepository
{
    private readonly DevComunityDbContext _context;

    public TagRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .Include(t => t.QuestionTags)
            .FirstOrDefaultAsync(t => t.TagId == id, cancellationToken);
    }

    public async Task<Tag?> GetByNameAsync(string tagName, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .Include(t => t.QuestionTags)
            .FirstOrDefaultAsync(t => t.TagName == tagName, cancellationToken);
    }

    public async Task<(IEnumerable<Tag> Items, int TotalCount)> GetPaginatedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string sort = "popular",
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tags
            .Include(t => t.QuestionTags)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => t.TagName.Contains(searchTerm) || 
                                     (t.Description != null && t.Description.Contains(searchTerm)));
        }

        query = sort switch
        {
            "name" => query.OrderBy(t => t.TagName),
            "newest" => query.OrderByDescending(t => t.TagId),
            _ => query.OrderByDescending(t => t.QuestionTags.Count) // popular
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await _context.Tags.AddAsync(tag, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task<Tag> GetOrCreateAsync(string tagName, CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagName == tagName, cancellationToken);
        if (tag == null)
        {
            tag = new Tag { TagName = tagName };
            await _context.Tags.AddAsync(tag, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        return tag;
    }

    public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .Include(t => t.QuestionTags)
            .OrderByDescending(t => t.QuestionTags.Count)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
