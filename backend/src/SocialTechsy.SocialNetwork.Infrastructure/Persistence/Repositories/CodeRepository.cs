using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Code Repository entity
/// </summary>
public class CodeRepository : ICodeRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public CodeRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Repository> Items, int TotalCount)> GetPaginatedAsync(
        int page, int pageSize, string? search = null, int? ownerId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Repositories
            .Include(r => r.Owner)
            .Where(r => !r.IsPrivate); // Only public repos by default

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search)));
        }

        if (ownerId.HasValue)
        {
            query = query.Where(r => r.OwnerId == ownerId.Value);
        }

        query = query.OrderByDescending(r => r.StarCount).ThenByDescending(r => r.CreatedDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Repository?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.RepositoryId == id, cancellationToken);
    }

    public async Task<IEnumerable<Repository>> GetByOwnerIdAsync(int ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Include(r => r.Owner)
            .Where(r => r.OwnerId == ownerId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        await _context.Repositories.AddAsync(repository, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return repository;
    }

    public async Task UpdateAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        _context.Repositories.Update(repository);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var repo = await _context.Repositories.FindAsync(new object[] { id }, cancellationToken);
        if (repo != null)
        {
            _context.Repositories.Remove(repo);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories.AnyAsync(r => r.RepositoryId == id, cancellationToken);
    }
}
