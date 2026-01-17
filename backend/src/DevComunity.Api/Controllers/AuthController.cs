using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Commands.Auth;
using DevComunity.Application.CommandHandlers.Auth;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Queries.Users;
using DevComunity.Application.QueryHandlers.Users;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Authentication - Uses CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly RegisterCommandHandler _registerHandler;
    private readonly LoginCommandHandler _loginHandler;
    private readonly GetCurrentUserQueryHandler _getCurrentUserHandler;

    public AuthController(
        ILogger<AuthController> logger,
        RegisterCommandHandler registerHandler,
        LoginCommandHandler loginHandler,
        GetCurrentUserQueryHandler getCurrentUserHandler)
    {
        _logger = logger;
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Validation failed"
            });
        }

        _logger.LogInformation("Registering new user: {Username}", command.Username);

        var result = await _registerHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Validation failed"
            });
        }

        _logger.LogInformation("Login attempt for: {Email}", command.Email);

        var result = await _loginHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
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
        // For now, return placeholder
        return Ok(new AuthResponse
        {
            Success = true,
            AccessToken = "new-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
    }

    /// <summary>
    /// Logout (invalidate refresh token)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User logout");

        // TODO: Implement LogoutCommandHandler to invalidate refresh token

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        // Get user ID from JWT claims
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
