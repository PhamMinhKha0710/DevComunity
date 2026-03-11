using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Queries.Search;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ILogger<SearchController> _logger;
    private readonly IMediator _mediator;

    public SearchController(ILogger<SearchController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

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

        var result = await _mediator.Send(new SearchQuery { Query = q, MaxResults = maxResults }, cancellationToken);
        return Ok(result);
    }
}
