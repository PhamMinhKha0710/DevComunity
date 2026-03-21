using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Tag;

namespace SocialTechsy.SocialNetwork.Application.Queries.Tags;

/// <summary>
/// Query for getting all tag preferences for a user
/// </summary>
public class GetUserTagPreferencesQuery : IRequest<IEnumerable<TagPreferenceDto>>
{
    public int UserId { get; set; }
}

/// <summary>
/// Query for getting followed tags for a user
/// </summary>
public class GetFollowedTagsQuery : IRequest<IEnumerable<TagPreferenceDto>>
{
    public int UserId { get; set; }
}
