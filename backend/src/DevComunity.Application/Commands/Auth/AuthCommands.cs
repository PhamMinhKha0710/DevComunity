using System.ComponentModel.DataAnnotations;

namespace DevComunity.Application.Commands.Auth;

/// <summary>
/// Command for user login
/// </summary>
public class LoginCommand
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Command for user registration
/// </summary>
public class RegisterCommand
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = null!;

    [StringLength(100)]
    public string? DisplayName { get; set; }
}

/// <summary>
/// Command for refreshing tokens
/// </summary>
public class RefreshTokenCommand
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
