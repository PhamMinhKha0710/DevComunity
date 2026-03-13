using Microsoft.EntityFrameworkCore;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public OutboxRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(o => o.ProcessedAt == null && o.RetryCount < 5)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(long id, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.ProcessedAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task IncrementRetryAsync(long id, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.RetryCount, o => o.RetryCount + 1), cancellationToken);
    }

    public async Task CleanupProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        await _context.OutboxMessages
            .Where(o => o.ProcessedAt != null && o.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
