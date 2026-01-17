using DevComunity.Application.Commands.Auth;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Application.Interfaces.Services;
using DevComunity.Domain.Entities;

namespace DevComunity.Application.CommandHandlers.Auth;

/// <summary>
/// Handler for RegisterCommand
/// </summary>
public class RegisterCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(command.Email, cancellationToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email already in use"
            };
        }

        // Check if username already exists
        if (await _userRepository.UsernameExistsAsync(command.Username, cancellationToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Username already taken"
            };
        }

        // Create user
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

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(createdUser.UserId, createdUser.Email, createdUser.Username);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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
