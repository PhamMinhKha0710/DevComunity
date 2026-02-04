using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Queries.Search;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Search;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

/// <summary>
/// API Controller for unified search across the platform
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ILogger<SearchController> _logger;
    private readonly SearchQueryHandler _searchHandler;

    public SearchController(
        ILogger<SearchController> logger,
        SearchQueryHandler searchHandler)
    {
        _logger = logger;
        _searchHandler = searchHandler;
    }

    /// <summary>
    /// Search across questions, users, and tags
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResultDto>> Search(
        [FromQuery] string q,
        [FromQuery] int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new SearchResultDto());

        _logger.LogInformation("Searching for: {Query}", q);

        var query = new SearchQuery
        {
            Query = q,
            MaxResults = maxResults
        };

        var result = await _searchHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }
}
