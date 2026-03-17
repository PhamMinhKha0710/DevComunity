using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Infrastructure.Services;

/// <summary>
/// Default implementation that issues JWT access tokens and persists refresh tokens.
/// </summary>
public class AuthTokenIssuer : IAuthTokenIssuer
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthTokenIssuer(
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<(string AccessToken, string RefreshToken)> IssueTokensAsync(
        User user,
        int refreshTokenDays,
        CancellationToken cancellationToken = default)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user.UserId, user.Email, user.Username);
        var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return (accessToken, refreshTokenString);
    }
}

