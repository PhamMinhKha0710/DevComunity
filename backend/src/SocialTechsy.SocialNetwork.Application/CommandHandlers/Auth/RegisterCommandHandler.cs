using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenIssuer _authTokenIssuer;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthTokenIssuer authTokenIssuer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authTokenIssuer = authTokenIssuer;
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

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            DisplayName = request.DisplayName ?? request.Username,
            CreatedDate = DateTime.UtcNow,
            ReputationPoints = 1,
            IsEmailVerified = false
        };

        var createdUser = await _userRepository.AddAsync(user, cancellationToken);

        var (accessToken, refreshTokenString) =
            await _authTokenIssuer.IssueTokensAsync(createdUser, 7, cancellationToken);

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
    }
}
