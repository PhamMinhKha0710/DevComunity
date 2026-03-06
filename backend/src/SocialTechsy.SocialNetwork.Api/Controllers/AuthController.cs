using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Queries.Users;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Users;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly RegisterCommandHandler _registerHandler;
    private readonly LoginCommandHandler _loginHandler;
    private readonly ForgotPasswordCommandHandler _forgotPasswordHandler;
    private readonly ResetPasswordCommandHandler _resetPasswordHandler;
    private readonly GetCurrentUserQueryHandler _getCurrentUserHandler;
    private readonly ForgotPasswordCommandHandler _forgotPasswordHandler;

    public AuthController(
        ILogger<AuthController> logger,
        RegisterCommandHandler registerHandler,
        LoginCommandHandler loginHandler,
        ForgotPasswordCommandHandler forgotPasswordHandler,
        ResetPasswordCommandHandler resetPasswordHandler,
        GetCurrentUserQueryHandler getCurrentUserHandler)
    {
        _logger = logger;
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _forgotPasswordHandler = forgotPasswordHandler;
        _resetPasswordHandler = resetPasswordHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _forgotPasswordHandler = forgotPasswordHandler;
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
        var result = await _registerHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
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
        var result = await _loginHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    /// <summary>
    /// Request password reset
    /// </summary>
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

        var result = await _forgotPasswordHandler.HandleAsync(command, cancellationToken);

        // Always return 200 to prevent email enumeration
        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing token");

        // TODO: Implement RefreshTokenCommandHandler
        return Ok(new AuthResponse
        {
            Success = true,
            AccessToken = "new-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User logout");
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Validation failed" });

        _logger.LogInformation("Password reset requested for: {Email}", command.Email);
        var result = await _forgotPasswordHandler.HandleAsync(command, cancellationToken);

        return Ok(result);
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
        var result = await _resetPasswordHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
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

        var query = new GetCurrentUserQuery { UserId = userId };
        var result = await _getCurrentUserHandler.HandleAsync(query, cancellationToken);

        if (result == null)
            return Unauthorized();

        return Ok(result);
    }
}
