namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

/// <summary>
/// Processes OAuth authentication callbacks and manages OAuth user lifecycle.
/// Implemented in Infrastructure layer as an adapter (avoids Application layer referencing Infrastructure).
/// </summary>
public interface IExternalAuthService
{
    /// <summary>
    /// Process an OAuth callback: find or create user, issue tokens, create session.
    /// Returns a result containing either success data or an error message.
    /// </summary>
    Task<OAuthProcessingResult> ProcessCallbackAsync(
        string provider,
        string providerKey,
        string? email,
        string name,
        string? avatarUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique username based on the OAuth name.
    /// </summary>
    Task<string> GenerateUniqueUsernameAsync(string baseName, string provider, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of OAuth callback processing
/// </summary>
public class OAuthProcessingResult
{
    public bool IsSuccess { get; set; }
    public int UserId { get; set; }
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string SessionCode { get; set; } = null!;
    public string? RedirectUrl { get; set; }
    public string? Error { get; set; }

    public static OAuthProcessingResult Success(int userId, string accessToken, string refreshToken, string sessionCode, string frontendBaseUrl, string provider)
    {
        return new OAuthProcessingResult
        {
            IsSuccess = true,
            UserId = userId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            SessionCode = sessionCode,
            RedirectUrl = $"{frontendBaseUrl}/auth/callback?success=true&code={Uri.EscapeDataString(sessionCode)}&message={Uri.EscapeDataString($"Logged in via {provider}")}"
        };
    }

    public static OAuthProcessingResult Failure(string error, string frontendBaseUrl)
    {
        return new OAuthProcessingResult
        {
            IsSuccess = false,
            Error = error,
            RedirectUrl = $"{frontendBaseUrl}/auth?error={Uri.EscapeDataString(error)}"
        };
    }
}
