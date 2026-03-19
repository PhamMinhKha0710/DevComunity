using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalAuthController : ControllerBase
{
    private readonly ILogger<ExternalAuthController> _logger;
    private readonly IExternalAuthService _externalAuthService;

    public ExternalAuthController(
        ILogger<ExternalAuthController> logger,
        IExternalAuthService externalAuthService)
    {
        _logger = logger;
        _externalAuthService = externalAuthService;
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

        var backendCallbackUrl = $"{Request.Scheme}://{Request.Host}/api/ExternalAuth/callback/{provider}";
        var properties = new AuthenticationProperties
        {
            RedirectUri = backendCallbackUrl,
            Items = { { "scheme", provider } }
        };

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
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExternalCallback(string provider, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(provider))
        {
            return Redirect("/auth?error=" + Uri.EscapeDataString("Unknown OAuth provider"));
        }

        const string externalCookieScheme = "ExternalCookie";
        var externalLoginInfo = await HttpContext.AuthenticateAsync(externalCookieScheme);

        if (externalLoginInfo?.Principal == null)
        {
            return Redirect("/auth?error=" + Uri.EscapeDataString("External authentication failed"));
        }

        var providerKey = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email);
        var name = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Name)
            ?? externalLoginInfo.Principal.FindFirstValue("name")
            ?? "User";
        var avatar = externalLoginInfo.Principal.FindFirstValue("urn:google:picture")
            ?? externalLoginInfo.Principal.FindFirstValue("picture")
            ?? externalLoginInfo.Principal.FindFirstValue("avatar")
            ?? externalLoginInfo.Principal.FindFirstValue("avatar_url")
            ?? (externalLoginInfo.Properties?.Items.TryGetValue("ExternalProviderAvatar", out var extAvatar) == true ? extAvatar : null);

        if (string.IsNullOrEmpty(providerKey))
        {
            return Redirect("/auth?error=" + Uri.EscapeDataString("Failed to get provider key"));
        }

        _logger.LogInformation("External login callback from {Provider} with email {Email}", provider, email);

        var result = await _externalAuthService.ProcessCallbackAsync(
            provider, providerKey, email, name, avatar, cancellationToken);

        if (string.IsNullOrEmpty(result.RedirectUrl))
            return Redirect("/auth?error=" + Uri.EscapeDataString(result.Error ?? "Unknown error"));

        return Redirect(result.RedirectUrl);
    }
}
