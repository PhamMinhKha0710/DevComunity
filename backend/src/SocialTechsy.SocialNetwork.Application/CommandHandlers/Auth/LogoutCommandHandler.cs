using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class LogoutCommandHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task HandleAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);
    }
}
