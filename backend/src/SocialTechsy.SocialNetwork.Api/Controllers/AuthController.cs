using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Queries.Users;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IMediator _mediator;

    public AuthController(
        ILogger<AuthController> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
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

        _logger.LogInformation("OAuth exchange requested");
        var result = await _mediator.Send(new ExchangeOAuthCodeCommand { Code = request.Code }, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        _logger.LogInformation("OAuth exchange succeeded");
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

    [HttpPost("forgot-password/request-code")]
    [ProducesResponseType(typeof(RequestForgotPasswordOtpResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestForgotPasswordOtpResponse>> RequestForgotPasswordOtp(
        [FromBody] RequestForgotPasswordOtpCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new RequestForgotPasswordOtpResponse
            {
                Success = false,
                Message = "Please provide a valid email address."
            });
        }

        _logger.LogInformation("Forgot password OTP requested for: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("forgot-password/confirm")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> ConfirmForgotPassword(
        [FromBody] ConfirmForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new AuthResponse { Success = false, Message = "Validation failed" });

        _logger.LogInformation("Forgot password OTP confirm for: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

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
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("request-password-change-code")]
    [ProducesResponseType(typeof(RequestPasswordChangeCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RequestPasswordChangeCodeResponse>> RequestPasswordChangeCode(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        _logger.LogInformation("Password change code requested for user {UserId}", userId);
        var result = await _mediator.Send(new RequestPasswordChangeCodeCommand { UserId = userId }, cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        command.UserId = userId;

        _logger.LogInformation("Password change attempt for user {UserId}", userId);
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
