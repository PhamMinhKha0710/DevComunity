using Microsoft.EntityFrameworkCore;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

public class OAuthLoginSessionRepository : IOAuthLoginSessionRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public OAuthLoginSessionRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    public async Task<OAuthLoginSession?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.OAuthLoginSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Code == code, cancellationToken);
    }

    public async Task<OAuthLoginSession> AddAsync(OAuthLoginSession session, CancellationToken cancellationToken = default)
    {
        _context.OAuthLoginSessions.Add(session);
        return session;
    }

    public async Task UpdateAsync(OAuthLoginSession session, CancellationToken cancellationToken = default)
    {
        _context.OAuthLoginSessions.Update(session);
    }

    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        await _context.OAuthLoginSessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow.AddDays(-1))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
