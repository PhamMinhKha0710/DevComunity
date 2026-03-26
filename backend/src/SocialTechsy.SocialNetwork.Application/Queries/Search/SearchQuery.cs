using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;

namespace SocialTechsy.SocialNetwork.Application.Queries.Search;

/// <summary>
/// Query for unified search across questions, users, and tags
/// </summary>
public class SearchQuery : IRequest<SearchResultDto>
{
    public string Query { get; set; } = null!;
    public int MaxResults { get; set; } = 5; // Results per type
}
