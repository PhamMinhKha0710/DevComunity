using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthTokenIssuer _authTokenIssuer;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAuthTokenIssuer authTokenIssuer)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _authTokenIssuer = authTokenIssuer;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (storedToken == null)
        {
            return new AuthResponse { Success = false, Message = "Invalid refresh token" };
        }

        if (storedToken.IsRevoked)
        {
            // Possible token reuse attack — revoke all tokens for this user
            await _refreshTokenRepository.RevokeAllByUserIdAsync(storedToken.UserId, cancellationToken);
            return new AuthResponse { Success = false, Message = "Token has been revoked. All sessions invalidated for security." };
        }

        if (storedToken.IsExpired)
        {
            return new AuthResponse { Success = false, Message = "Refresh token has expired. Please login again." };
        }

        var user = storedToken.User;
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "User not found" };
        }

        // Rotate: revoke old token and create new one
        var (newAccessToken, newRefreshTokenString) =
            await _authTokenIssuer.IssueTokensAsync(user, 7, cancellationToken);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshTokenString;
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                ProfilePicture = user.ProfilePicture,
                ReputationPoints = user.ReputationPoints,
                IsEmailVerified = user.IsEmailVerified
            }
        };
    }
}
