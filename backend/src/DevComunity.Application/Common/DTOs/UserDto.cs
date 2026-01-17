namespace DevComunity.Application.Common.DTOs;

/// <summary>
/// User data transfer object
/// </summary>
public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
    public int ReputationPoints { get; set; }
    public bool IsEmailVerified { get; set; }
}
