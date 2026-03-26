using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

namespace SocialTechsy.SocialNetwork.Application.Commands.Auth;

/// <summary>
/// Command for user login
/// </summary>
public class LoginCommand : IRequest<AuthResponse>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Command for user registration
/// </summary>
public class RegisterCommand : IRequest<AuthResponse>
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public string? DisplayName { get; set; }
}

public class RefreshTokenCommand : IRequest<AuthResponse>
{
    public string RefreshToken { get; set; } = null!;
}

/// <summary>
/// Command for forgot password
/// </summary>
public class ForgotPasswordCommand : IRequest<ForgotPasswordResponse>
{
    public string Email { get; set; } = null!;
}

/// <summary>
/// Command for resetting password with token
/// </summary>
public class ResetPasswordCommand : IRequest<AuthResponse>
{
    public string Token { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class LogoutCommand : IRequest
{
    public int UserId { get; set; }
}

/// <summary>
/// Command for exchanging an OAuth code for authentication tokens
/// </summary>
public class ExchangeOAuthCodeCommand : IRequest<AuthResponse>
{
    public string Code { get; set; } = null!;
}

/// <summary>
/// Command for requesting a password change verification code
/// </summary>
public class RequestPasswordChangeCodeCommand : IRequest<RequestPasswordChangeCodeResponse>
{
    public int UserId { get; set; }
}

/// <summary>
/// Response for request-password-change-code
/// </summary>
public class RequestPasswordChangeCodeResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Command for changing password with email verification code
/// </summary>
public class ChangePasswordCommand : IRequest<AuthResponse>
{
    public int UserId { get; set; }
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public string VerificationCode { get; set; } = null!;
}

/// <summary>
/// Command for requesting an OTP for forgot password (step 1 of 2)
/// </summary>
public class RequestForgotPasswordOtpCommand : IRequest<RequestForgotPasswordOtpResponse>
{
    public string Email { get; set; } = null!;
}

public class RequestForgotPasswordOtpResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Command for confirming forgot password with OTP (step 2 of 2)
/// </summary>
public class ConfirmForgotPasswordCommand : IRequest<AuthResponse>
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}
