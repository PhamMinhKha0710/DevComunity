using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Users;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IMediator _mediator;
    private readonly IOAuthLoginSessionRepository _oauthSessionRepository;

    public AuthController(
        ILogger<AuthController> logger,
        IMediator mediator,
        IOAuthLoginSessionRepository oauthSessionRepository)
    {
        _logger = logger;
        _mediator = mediator;
        _oauthSessionRepository = oauthSessionRepository;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new AuthResponse { Success = false, Message = "Validation failed" });

        _logger.LogInformation("Registering new user: {Username}", command.Username);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("exchange")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> ExchangeCode(
        [FromBody] AuthCodeExchangeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new AuthResponse { Success = false, Message = "Code is required" });

        var session = await _oauthSessionRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("OAuth exchange attempted with unknown code");
            return BadRequest(new AuthResponse { Success = false, Message = "Invalid or expired code" });
        }

        if (session.IsUsed)
        {
            _logger.LogWarning("OAuth exchange attempted with reused code for user {UserId}", session.UserId);
            return BadRequest(new AuthResponse { Success = false, Message = "Code has already been used" });
        }

        if (session.IsExpired)
        {
            _logger.LogWarning("OAuth exchange attempted with expired code for user {UserId}", session.UserId);
            return BadRequest(new AuthResponse { Success = false, Message = "Code has expired" });
        }

        session.IsUsed = true;
        session.UsedAt = DateTime.UtcNow;
        await _oauthSessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation("OAuth code exchanged successfully for user {UserId}", session.UserId);

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Authentication successful",
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = session.User.ToDto()
        });
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new AuthResponse { Success = false, Message = "Validation failed" });

        _logger.LogInformation("Login attempt for: {Email}", command.Email);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Please provide a valid email address" });
        }

        _logger.LogInformation("Password reset requested for: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new { success = true, message = result.Message });
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new AuthResponse { Success = false, Message = "Validation failed" });

        _logger.LogInformation("Password reset attempt");
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing token");

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        _logger.LogInformation("User {UserId} logging out", userId);

        await _mediator.Send(new LogoutCommand { UserId = userId }, cancellationToken);

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetCurrentUserQuery { UserId = userId }, cancellationToken);

        if (result == null)
            return Unauthorized();

        return Ok(result);
    }
}
