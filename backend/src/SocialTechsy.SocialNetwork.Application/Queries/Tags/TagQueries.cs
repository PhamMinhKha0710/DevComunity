using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Tag;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;

namespace SocialTechsy.SocialNetwork.Application.Queries.Tags;

/// <summary>
/// Query for getting paginated tags
/// </summary>
public class GetTagsQuery : IRequest<PaginatedResponse<TagDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 36;
    public string? Search { get; set; }
    public string Sort { get; set; } = "popular"; // popular, name, newest
}

/// <summary>
/// Query for getting a tag by name
/// </summary>
public class GetTagByNameQuery : IRequest<TagDto?>
{
    public string TagName { get; set; } = null!;
}
