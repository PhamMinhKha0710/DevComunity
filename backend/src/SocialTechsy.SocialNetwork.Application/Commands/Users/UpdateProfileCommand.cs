using System.ComponentModel.DataAnnotations;
using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Users;

/// <summary>
/// Command for updating user profile
/// </summary>
public class UpdateProfileCommand : IRequest<bool>
{
    public int UserId { get; set; }

    [StringLength(100)]
    public string? DisplayName { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [StringLength(200)]
    [Url]
    public string? Website { get; set; }
}
