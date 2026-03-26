using Microsoft.EntityFrameworkCore;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

public class EmailOutboxRepository : IEmailOutboxRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public EmailOutboxRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    public async Task<EmailOutbox> AddAsync(EmailOutbox email, CancellationToken cancellationToken = default)
    {
        await _context.EmailOutbox.AddAsync(email, cancellationToken);
        return email;
    }

    public async Task<List<EmailOutbox>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.EmailOutbox
            .Where(e => e.Status == EmailOutboxStatus.Pending && e.RetryCount < 5
                        && (e.NextRetryAt == null || e.NextRetryAt <= now))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkSentAsync(int id, CancellationToken cancellationToken = default)
    {
        await _context.EmailOutbox
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, EmailOutboxStatus.Sent)
                .SetProperty(e => e.ProcessedAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task MarkFailedAsync(int id, string error, CancellationToken cancellationToken = default)
    {
        await _context.EmailOutbox
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, EmailOutboxStatus.Failed)
                .SetProperty(e => e.ErrorMessage, error), cancellationToken);
    }

    public async Task IncrementRetryAsync(int id, CancellationToken cancellationToken = default)
    {
        var nextRetry = DateTime.UtcNow.AddMinutes(Math.Pow(2, 1)); // exponential backoff: 2, 4, 8...
        await _context.EmailOutbox
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.RetryCount, e => e.RetryCount + 1)
                .SetProperty(e => e.NextRetryAt, nextRetry), cancellationToken);
    }
}
