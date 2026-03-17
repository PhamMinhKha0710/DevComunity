using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

public interface IOAuthLoginSessionRepository
{
    Task<OAuthLoginSession?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<OAuthLoginSession> AddAsync(OAuthLoginSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(OAuthLoginSession session, CancellationToken cancellationToken = default);
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}
