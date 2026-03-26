using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

public interface IEmailOutboxRepository
{
    Task<EmailOutbox> AddAsync(EmailOutbox email, CancellationToken cancellationToken = default);
    Task<List<EmailOutbox>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkSentAsync(int id, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(int id, string error, CancellationToken cancellationToken = default);
    Task IncrementRetryAsync(int id, CancellationToken cancellationToken = default);
}
