using Microsoft.EntityFrameworkCore;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public PasswordResetTokenRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Token == token, cancellationToken);
    }

    public async Task<PasswordResetToken> AddAsync(PasswordResetToken resetToken, CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync(cancellationToken);
        return resetToken;
    }

    public async Task UpdateAsync(PasswordResetToken resetToken, CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Update(resetToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task InvalidateAllByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens
            .Where(p => p.UserId == userId && !p.IsUsed)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsUsed, true), cancellationToken);
    }
}
