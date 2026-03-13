using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(long id, CancellationToken cancellationToken = default);
    Task IncrementRetryAsync(long id, CancellationToken cancellationToken = default);
    Task CleanupProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
