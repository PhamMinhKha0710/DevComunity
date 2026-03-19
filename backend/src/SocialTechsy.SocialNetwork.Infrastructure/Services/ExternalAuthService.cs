using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Infrastructure.Services;

public class ExternalAuthService : IExternalAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IOAuthLoginSessionRepository _oauthSessionRepository;
    private readonly IAuthTokenIssuer _authTokenIssuer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        IUserRepository userRepository,
        IOAuthLoginSessionRepository oauthSessionRepository,
        IAuthTokenIssuer authTokenIssuer,
        IConfiguration configuration,
        ILogger<ExternalAuthService> logger)
    {
        _userRepository = userRepository;
        _oauthSessionRepository = oauthSessionRepository;
        _authTokenIssuer = authTokenIssuer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OAuthProcessingResult> ProcessCallbackAsync(
        string provider,
        string providerKey,
        string? email,
        string name,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing OAuth callback from {Provider} with email {Email}", provider, email);

        var user = await _userRepository.GetByExternalProviderAsync(provider, providerKey, cancellationToken);

        if (user == null)
        {
            if (string.IsNullOrEmpty(email))
            {
                return OAuthProcessingResult.Failure("Email not provided by OAuth provider",
                    _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000");
            }

            var existingUserByEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUserByEmail != null)
            {
                existingUserByEmail.LinkOAuthProvider(provider, providerKey, avatarUrl);
                await _userRepository.UpdateAsync(existingUserByEmail, cancellationToken);
                user = existingUserByEmail;
            }
            else
            {
                var username = await GenerateUniqueUsernameAsync(name, provider, cancellationToken);
                user = User.Create(username, email, "oauth-user-no-password", name);
                user.SetOAuthProvider(provider, providerKey, avatarUrl);
                user.VerifyEmail();
                user.SetProfilePicture(avatarUrl);
                user = await _userRepository.AddAsync(user, cancellationToken);
            }
        }
        else
        {
            user.RecordLogin();
            if (!string.IsNullOrEmpty(avatarUrl) && string.IsNullOrEmpty(user.ProfilePicture))
            {
                user.SetProfilePicture(avatarUrl);
            }
            user.LinkOAuthProvider(provider, providerKey, avatarUrl);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        var (accessToken, refreshTokenString) = await _authTokenIssuer.IssueTokensAsync(user, 7, cancellationToken);

        _logger.LogInformation("User {UserId} logged in via {Provider}", user.UserId, provider);

        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var session = new OAuthLoginSession
        {
            Code = code,
            UserId = user.UserId,
            Provider = provider,
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        await _oauthSessionRepository.AddAsync(session, cancellationToken);

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
        return OAuthProcessingResult.Success(user.UserId, accessToken, refreshTokenString, code, frontendBaseUrl, provider);
    }

    public async Task<string> GenerateUniqueUsernameAsync(string baseName, string provider, CancellationToken cancellationToken = default)
    {
        var cleanName = baseName.ToLower()
            .Replace(" ", "")
            .Replace(".", "")
            .Replace("-", "");

        if (string.IsNullOrEmpty(cleanName))
        {
            cleanName = provider + "user";
        }

        var username = cleanName;
        var counter = 1;

        while (await _userRepository.GetByUsernameAsync(username, cancellationToken) != null)
        {
            username = $"{cleanName}{counter}";
            counter++;
        }

        return username;
    }
}
