using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Repositories;

/// <summary>
/// Command for creating a repository
/// </summary>
public class CreateRepositoryCommand
{
    public int OwnerId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
}

/// <summary>
/// Handler for creating a repository
/// </summary>
public class CreateRepositoryCommandHandler
{
    private readonly ICodeRepository _codeRepository;

    public CreateRepositoryCommandHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<RepositoryDto> HandleAsync(CreateRepositoryCommand command, CancellationToken cancellationToken)
    {
        var repository = new Repository
        {
            OwnerId = command.OwnerId,
            Name = command.Name,
            Description = command.Description,
            IsPrivate = command.IsPrivate,
            DefaultBranch = "main",
            CreatedDate = DateTime.UtcNow,
            StarCount = 0,
            ForkCount = 0
        };

        var created = await _codeRepository.AddAsync(repository, cancellationToken);

        return new RepositoryDto
        {
            RepositoryId = created.RepositoryId,
            Name = created.Name,
            Description = created.Description,
            CloneUrl = created.CloneUrl,
            DefaultBranch = created.DefaultBranch,
            IsPrivate = created.IsPrivate,
            StarCount = created.StarCount,
            ForkCount = created.ForkCount,
            CreatedDate = created.CreatedDate,
            OwnerId = created.OwnerId
        };
    }
}

/// <summary>
/// Command for updating a repository
/// </summary>
public class UpdateRepositoryCommand
{
    public int RepositoryId { get; set; }
    public int UserId { get; set; }
    public string? Description { get; set; }
    public bool? IsPrivate { get; set; }
}

/// <summary>
/// Handler for updating a repository
/// </summary>
public class UpdateRepositoryCommandHandler
{
    private readonly ICodeRepository _codeRepository;

    public UpdateRepositoryCommandHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<bool> HandleAsync(UpdateRepositoryCommand command, CancellationToken cancellationToken)
    {
        var repository = await _codeRepository.GetByIdAsync(command.RepositoryId, cancellationToken);
        if (repository == null || repository.OwnerId != command.UserId)
            return false;

        if (command.Description != null)
            repository.Description = command.Description;
        if (command.IsPrivate.HasValue)
            repository.IsPrivate = command.IsPrivate.Value;

        repository.LastUpdatedDate = DateTime.UtcNow;

        await _codeRepository.UpdateAsync(repository, cancellationToken);
        return true;
    }
}

/// <summary>
/// Command for deleting a repository
/// </summary>
public class DeleteRepositoryCommand
{
    public int RepositoryId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Handler for deleting a repository
/// </summary>
public class DeleteRepositoryCommandHandler
{
    private readonly ICodeRepository _codeRepository;

    public DeleteRepositoryCommandHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<bool> HandleAsync(DeleteRepositoryCommand command, CancellationToken cancellationToken)
    {
        var repository = await _codeRepository.GetByIdAsync(command.RepositoryId, cancellationToken);
        if (repository == null || repository.OwnerId != command.UserId)
            return false;

        await _codeRepository.DeleteAsync(command.RepositoryId, cancellationToken);
        return true;
    }
}
