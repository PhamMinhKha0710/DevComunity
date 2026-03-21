using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenIssuer _authTokenIssuer;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthTokenIssuer authTokenIssuer,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authTokenIssuer = authTokenIssuer;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email already in use"
            };
        }

        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Username already taken"
            };
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var user = User.Create(
                request.Username,
                request.Email,
                _passwordHasher.HashPassword(request.Password),
                request.DisplayName);

            var createdUser = await _userRepository.AddAsync(user, ct);

            // Persist user to get its ID before creating tokens (RefreshToken has FK to UserId)
            await _unitOfWork.SaveChangesAsync(ct);

            var (accessToken, refreshTokenString) =
                await _authTokenIssuer.IssueTokensAsync(createdUser, 7, ct);

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserDto
                {
                    UserId = createdUser.UserId,
                    Username = createdUser.Username,
                    Email = createdUser.Email,
                    DisplayName = createdUser.DisplayName,
                    ReputationPoints = createdUser.ReputationPoints,
                    IsEmailVerified = createdUser.IsEmailVerified
                }
            };
        }, cancellationToken);
    }
}
