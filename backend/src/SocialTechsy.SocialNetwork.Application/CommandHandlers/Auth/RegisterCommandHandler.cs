using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class RegisterCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.EmailExistsAsync(command.Email, cancellationToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email already in use"
            };
        }

        if (await _userRepository.UsernameExistsAsync(command.Username, cancellationToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Username already taken"
            };
        }

        var user = new User
        {
            Username = command.Username,
            Email = command.Email,
            PasswordHash = _passwordHasher.HashPassword(command.Password),
            DisplayName = command.DisplayName ?? command.Username,
            CreatedDate = DateTime.UtcNow,
            ReputationPoints = 1,
            IsEmailVerified = false
        };

        var createdUser = await _userRepository.AddAsync(user, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(createdUser.UserId, createdUser.Email, createdUser.Username);
        var refreshTokenString = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = createdUser.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

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
