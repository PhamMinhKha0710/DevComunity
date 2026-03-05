using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Repositories;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Repositories;
using SocialTechsy.SocialNetwork.Infrastructure.External.Gitea;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

/// <summary>
/// API Controller for Code Repositories (Gitea integration)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RepositoriesController : ControllerBase
{
    private readonly ILogger<RepositoriesController> _logger;
    private readonly GetRepositoriesQueryHandler _getRepositoriesHandler;
    private readonly GetRepositoryByIdQueryHandler _getRepositoryByIdHandler;
    private readonly CreateRepositoryCommandHandler _createHandler;
    private readonly UpdateRepositoryCommandHandler _updateHandler;
    private readonly DeleteRepositoryCommandHandler _deleteHandler;
    private readonly IGiteaService _giteaService;

    public RepositoriesController(
        ILogger<RepositoriesController> logger,
        GetRepositoriesQueryHandler getRepositoriesHandler,
        GetRepositoryByIdQueryHandler getRepositoryByIdHandler,
        CreateRepositoryCommandHandler createHandler,
        UpdateRepositoryCommandHandler updateHandler,
        DeleteRepositoryCommandHandler deleteHandler,
        IGiteaService giteaService)
    {
        _logger = logger;
        _getRepositoriesHandler = getRepositoriesHandler;
        _getRepositoryByIdHandler = getRepositoryByIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _giteaService = giteaService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get all public repositories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<RepositoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<RepositoryDto>>> GetRepositories(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting repositories, search: {Search}", search);

        var result = await _getRepositoriesHandler.HandleAsync(page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get repository by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RepositoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepositoryDto>> GetRepository(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting repository {RepositoryId}", id);

        var result = await _getRepositoryByIdHandler.HandleAsync(id, cancellationToken);
        if (result == null)
            return NotFound(new { message = $"Repository with ID {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get repository files/tree from Gitea
    /// </summary>
    [HttpGet("{id:int}/files")]
    [ProducesResponseType(typeof(IEnumerable<RepositoryFileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IEnumerable<RepositoryFileDto>>> GetRepositoryFiles(
        int id,
        [FromQuery] string? path = null,
        [FromQuery] string branch = "main",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting files for repository {RepositoryId}, path: {Path}, branch: {Branch}", id, path, branch);

        // Check if Gitea is configured
        if (!_giteaService.IsConfigured)
        {
            _logger.LogWarning("Gitea service is not configured");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { message = "Git integration is not configured" });
        }

        // Get repository from database to get Gitea info
        var repository = await _getRepositoryByIdHandler.HandleAsync(id, cancellationToken);
        if (repository == null)
            return NotFound(new { message = $"Repository with ID {id} not found" });

        // If path is specified, get directory contents
        if (!string.IsNullOrEmpty(path))
        {
            var contents = await _giteaService.GetDirectoryContentsAsync(
                repository.OwnerUsername, 
                repository.Name, 
                path, 
                branch, 
                cancellationToken);

            var files = contents.Select(c => new RepositoryFileDto
            {
                Name = c.Name,
                Path = c.Path,
                Type = c.Type == "dir" ? "tree" : "blob",
                Size = c.Size,
                Sha = c.Sha
            }).ToList();

            return Ok(files);
        }

        // Get root contents
        var rootContents = await _giteaService.GetDirectoryContentsAsync(
            repository.OwnerUsername, 
            repository.Name, 
            null, 
            branch, 
            cancellationToken);

        var rootFiles = rootContents.Select(c => new RepositoryFileDto
        {
            Name = c.Name,
            Path = c.Path,
            Type = c.Type == "dir" ? "tree" : "blob",
            Size = c.Size,
            Sha = c.Sha
        }).ToList();

        return Ok(rootFiles);
    }

    /// <summary>
    /// Get file content from Gitea
    /// </summary>
    [HttpGet("{id:int}/files/content")]
    [ProducesResponseType(typeof(FileContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FileContentDto>> GetFileContent(
        int id,
        [FromQuery] string path,
        [FromQuery] string branch = "main",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting file content for repository {RepositoryId}, path: {Path}, branch: {Branch}", id, path, branch);

        if (string.IsNullOrEmpty(path))
            return BadRequest(new { message = "Path is required" });

        if (!_giteaService.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { message = "Git integration is not configured" });
        }

        // Get repository info
        var repository = await _getRepositoryByIdHandler.HandleAsync(id, cancellationToken);
        if (repository == null)
            return NotFound(new { message = $"Repository with ID {id} not found" });

        // Get file content from Gitea
        var content = await _giteaService.GetFileContentAsync(
            repository.OwnerUsername, 
            repository.Name, 
            path, 
            branch, 
            cancellationToken);

        if (content == null)
            return NotFound(new { message = $"File not found: {path}" });

        // Decode base64 content if present
        var decodedContent = "";
        if (!string.IsNullOrEmpty(content.Content) && content.Encoding == "base64")
        {
            try
            {
                var bytes = Convert.FromBase64String(content.Content);
                decodedContent = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                decodedContent = content.Content; // Return as-is if decoding fails
            }
        }
        else
        {
            decodedContent = content.Content ?? "";
        }

        return Ok(new FileContentDto 
        { 
            Path = content.Path, 
            Content = decodedContent, 
            Encoding = "utf-8",
            Size = content.Size,
            Sha = content.Sha
        });
    }

    /// <summary>
    /// Get repository commits from Gitea
    /// </summary>
    [HttpGet("{id:int}/commits")]
    [ProducesResponseType(typeof(PaginatedResponse<CommitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PaginatedResponse<CommitDto>>> GetCommits(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? branch = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting commits for repository {RepositoryId}, page: {Page}", id, page);

        if (!_giteaService.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { message = "Git integration is not configured" });
        }

        // Get repository info
        var repository = await _getRepositoryByIdHandler.HandleAsync(id, cancellationToken);
        if (repository == null)
            return NotFound(new { message = $"Repository with ID {id} not found" });

        // Get commits from Gitea
        var giteaCommits = await _giteaService.GetCommitsAsync(
            repository.OwnerUsername, 
            repository.Name, 
            page, 
            pageSize, 
            branch, 
            cancellationToken);

        var commits = giteaCommits.Select(c => new CommitDto
        {
            Sha = c.Sha,
            Message = c.Commit.Message,
            AuthorName = c.Commit.Author.Name,
            AuthorEmail = c.Commit.Author.Email,
            AuthorAvatar = c.Author?.AvatarUrl,
            CommittedAt = c.Commit.Author.Date,
            Url = c.HtmlUrl
        }).ToList();

        return Ok(new PaginatedResponse<CommitDto>
        {
            Items = commits,
            Page = page,
            PageSize = pageSize,
            TotalCount = commits.Count // Note: Gitea doesn't return total count in list endpoint
        });
    }

    /// <summary>
    /// Create a new repository
    /// </summary>
    [HttpPost]

    [Authorize]
    [ProducesResponseType(typeof(RepositoryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RepositoryDto>> CreateRepository(
        [FromBody] CreateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} creating repository: {Name}", userId, request.Name);

        var command = new CreateRepositoryCommand
        {
            OwnerId = userId,
            Name = request.Name,
            Description = request.Description,
            IsPrivate = request.IsPrivate
        };

        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return Created($"/api/repositories/{result.RepositoryId}", result);
    }

    /// <summary>
    /// Update repository settings
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRepository(
        int id,
        [FromBody] UpdateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} updating repository {RepositoryId}", userId, id);

        var command = new UpdateRepositoryCommand
        {
            RepositoryId = id,
            UserId = userId,
            Description = request.Description,
            IsPrivate = request.IsPrivate
        };

        var success = await _updateHandler.HandleAsync(command, cancellationToken);
        if (!success)
            return NotFound(new { message = "Repository not found or access denied" });

        return Ok(new { message = "Repository updated" });
    }

    /// <summary>
    /// Delete a repository
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRepository(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} deleting repository {RepositoryId}", userId, id);

        var command = new DeleteRepositoryCommand
        {
            RepositoryId = id,
            UserId = userId
        };

        var success = await _deleteHandler.HandleAsync(command, cancellationToken);
        if (!success)
            return NotFound(new { message = "Repository not found or access denied" });

        return NoContent();
    }
}

public class CreateRepositoryRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; } = false;
}

public class UpdateRepositoryRequest
{
    public string? Description { get; set; }
    public bool? IsPrivate { get; set; }
}
