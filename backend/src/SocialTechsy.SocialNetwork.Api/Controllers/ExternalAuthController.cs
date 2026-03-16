using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalAuthController : ControllerBase
{
    private readonly ILogger<ExternalAuthController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public ExternalAuthController(
        ILogger<ExternalAuthController> logger,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService tokenService,
        IConfiguration configuration)
    {
        _logger = logger;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    [HttpGet("login/{provider}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ExternalLogin(string provider)
    {
        var supportedProviders = new[] { "google", "github", "facebook" };
        if (!supportedProviders.Contains(provider.ToLower()))
        {
            return BadRequest(new { message = "Unsupported OAuth provider" });
        }

        // Must match CallbackPath in Program.cs for each provider (e.g. /api/auth/external-callback/google)
        // RedirectUri must point to backend callback so user creation and token generation run before redirecting to frontend
        var backendCallbackUrl = $"{Request.Scheme}://{Request.Host}/api/ExternalAuth/callback/{provider}";
        var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = backendCallbackUrl,
            Items = { { "scheme", provider } }
        };

        // Use proper scheme names (case-sensitive): Google, GitHub, Facebook
        var scheme = provider.ToLower() switch
        {
            "google" => "Google",
            "github" => "GitHub",
            "facebook" => "Facebook",
            _ => provider
        };
        return Challenge(properties, scheme);
    }

    [HttpGet("callback/{provider}")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExternalCallback(string provider, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(provider))
        {
            return Redirect($"/auth?error={Uri.EscapeDataString("Unknown OAuth provider")}");
        }

        // Use ExternalCookie - OAuth middleware stores the result there (SignInScheme)
        const string externalCookieScheme = "ExternalCookie";
        var externalLoginInfo = await HttpContext.AuthenticateAsync(externalCookieScheme);

        if (externalLoginInfo?.Principal == null)
        {
            return Redirect($"/auth?error={Uri.EscapeDataString("External authentication failed")}");
        }

        var providerKey = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email);
        var name = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Name)
            ?? externalLoginInfo.Principal.FindFirstValue("name")
            ?? "User";
        // Google: "urn:google:picture" or "picture"; GitHub: "avatar_url"; Properties: "ExternalProviderAvatar"
        var avatar = externalLoginInfo.Principal.FindFirstValue("urn:google:picture")
            ?? externalLoginInfo.Principal.FindFirstValue("picture")
            ?? externalLoginInfo.Principal.FindFirstValue("avatar")
            ?? externalLoginInfo.Principal.FindFirstValue("avatar_url")
            ?? (externalLoginInfo.Properties?.Items.TryGetValue("ExternalProviderAvatar", out var extAvatar) == true ? extAvatar : null);

        if (string.IsNullOrEmpty(providerKey))
        {
            return Redirect($"/auth?error={Uri.EscapeDataString("Failed to get provider key")}");
        }

        _logger.LogInformation("External login callback from {Provider} with email {Email}", provider, email);

        var user = await _userRepository.GetByExternalProviderAsync(provider, providerKey, cancellationToken);

        if (user == null)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Redirect($"/auth?error={Uri.EscapeDataString("Email not provided by OAuth provider")}");
            }

            var existingUserByEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUserByEmail != null)
            {
                existingUserByEmail.ExternalProvider = provider;
                existingUserByEmail.ExternalProviderId = providerKey;
                existingUserByEmail.ExternalProviderAvatar = avatar;
                existingUserByEmail.LastLoginDate = DateTime.UtcNow;
                await _userRepository.UpdateAsync(existingUserByEmail, cancellationToken);
                user = existingUserByEmail;
            }
            else
            {
                var username = GenerateUsername(name, provider);
                user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = "oauth-user-no-password",
                    DisplayName = name,
                    ProfilePicture = avatar,
                    ExternalProvider = provider,
                    ExternalProviderId = providerKey,
                    ExternalProviderAvatar = avatar,
                    IsEmailVerified = true,
                    CreatedDate = DateTime.UtcNow,
                    LastLoginDate = DateTime.UtcNow,
                    ReputationPoints = 1
                };
                user = await _userRepository.AddAsync(user, cancellationToken);
            }
        }
        else
        {
            user.LastLoginDate = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(avatar) && string.IsNullOrEmpty(user.ProfilePicture))
            {
                user.ProfilePicture = avatar;
            }
            user.ExternalProviderAvatar = avatar;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        var accessToken = _tokenService.GenerateAccessToken(user.UserId, user.Email, user.Username);
        var refreshTokenString = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        _logger.LogInformation("User {UserId} logged in via {Provider}", user.UserId, provider);

        // Redirect to frontend callback page with tokens
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
        var redirectUrl = $"{frontendBaseUrl}/auth/callback?success=true&accessToken={Uri.EscapeDataString(accessToken)}&refreshToken={Uri.EscapeDataString(refreshTokenString)}&message={Uri.EscapeDataString($"Logged in via {provider}")}";
        return Redirect(redirectUrl);
    }

    private string GenerateUsername(string name, string provider)
    {
        var baseUsername = name.ToLower()
            .Replace(" ", "")
            .Replace(".", "")
            .Replace("-", "");

        if (string.IsNullOrEmpty(baseUsername))
        {
            baseUsername = provider + "user";
        }

        var username = baseUsername;
        var counter = 1;

        while (_userRepository.GetByUsernameAsync(username, CancellationToken.None).Result != null)
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        return username;
    }
}
