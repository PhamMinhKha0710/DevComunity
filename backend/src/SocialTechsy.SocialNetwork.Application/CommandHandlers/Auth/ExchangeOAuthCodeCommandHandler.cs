using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class ExchangeOAuthCodeCommandHandler : IRequestHandler<ExchangeOAuthCodeCommand, AuthResponse>
{
    private readonly IOAuthLoginSessionRepository _oauthSessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExchangeOAuthCodeCommandHandler(
        IOAuthLoginSessionRepository oauthSessionRepository,
        IUnitOfWork unitOfWork)
    {
        _oauthSessionRepository = oauthSessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(ExchangeOAuthCodeCommand request, CancellationToken cancellationToken)
    {
        var session = await _oauthSessionRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (session == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid or expired code"
            };
        }

        if (session.IsUsed)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Code has already been used"
            };
        }

        if (session.IsExpired)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Code has expired"
            };
        }

        session.IsUsed = true;
        session.UsedAt = DateTime.UtcNow;
        await _oauthSessionRepository.UpdateAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "Authentication successful",
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                UserId = session.User.UserId,
                Username = session.User.Username,
                Email = session.User.Email,
                DisplayName = session.User.DisplayName,
                ProfilePicture = session.User.ProfilePicture,
                ReputationPoints = session.User.ReputationPoints,
                IsEmailVerified = session.User.IsEmailVerified
            }
        };
    }
}
