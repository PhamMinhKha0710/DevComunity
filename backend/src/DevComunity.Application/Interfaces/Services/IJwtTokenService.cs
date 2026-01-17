namespace DevComunity.Application.Interfaces.Services;

/// <summary>
/// Service interface for JWT token generation
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(int userId, string email, string username);
    string GenerateRefreshToken();
    int? ValidateAccessToken(string token);
    bool ValidateRefreshToken(string token);
}
