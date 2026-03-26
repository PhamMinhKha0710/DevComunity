using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Users;

/// <summary>
/// Command for updating user profile
/// </summary>
public class UpdateProfileCommand : IRequest<bool>
{
    public int UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? ProfilePicture { get; set; }
}
