using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

/// <summary>
/// Issues access/refresh token pairs for a given user in a unified way.
/// Centralizes JWT + refresh-token creation logic so all auth flows stay consistent.
/// </summary>
public interface IAuthTokenIssuer
{
    Task<(string AccessToken, string RefreshToken)> IssueTokensAsync(
        User user,
        int refreshTokenDays,
        CancellationToken cancellationToken = default);
}

