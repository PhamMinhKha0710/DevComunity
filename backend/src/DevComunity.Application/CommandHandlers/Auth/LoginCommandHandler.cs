using DevComunity.Application.Commands.Auth;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Application.Interfaces.Services;

namespace DevComunity.Application.CommandHandlers.Auth;

/// <summary>
/// Handler for LoginCommand
/// </summary>
public class LoginCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        // Find user by email
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        // Update last login
        user.LastLoginDate = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user.UserId, user.Email, user.Username);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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
