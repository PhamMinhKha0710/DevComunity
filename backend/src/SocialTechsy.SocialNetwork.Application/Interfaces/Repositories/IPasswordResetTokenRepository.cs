using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<PasswordResetToken> AddAsync(PasswordResetToken resetToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(PasswordResetToken resetToken, CancellationToken cancellationToken = default);
    Task InvalidateAllByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
