using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Tag;

namespace SocialTechsy.SocialNetwork.Application.Commands.Tags;

/// <summary>
/// Command for following a tag
/// </summary>
public class FollowTagCommand : IRequest<TagPreferenceDto>
{
    public int TagId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command for ignoring a tag
/// </summary>
public class IgnoreTagCommand : IRequest<TagPreferenceDto>
{
    public int TagId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command for removing a tag preference
/// </summary>
public class RemoveTagPreferenceCommand : IRequest<bool>
{
    public int TagId { get; set; }
    public int UserId { get; set; }
}
