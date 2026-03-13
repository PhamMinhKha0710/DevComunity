using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Repositories;

/// <summary>
/// Command for creating a repository
/// </summary>
public class CreateRepositoryCommand : IRequest<RepositoryDto>
{
    public int OwnerId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
}

/// <summary>
/// Handler for creating a repository
/// </summary>
public class CreateRepositoryCommandHandler : IRequestHandler<CreateRepositoryCommand, RepositoryDto>
{
    private readonly ICodeRepository _codeRepository;

    public CreateRepositoryCommandHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<RepositoryDto> Handle(CreateRepositoryCommand request, CancellationToken cancellationToken)
    {
        var repository = new Repository
        {
            OwnerId = request.OwnerId,
            Name = request.Name,
            Description = request.Description,
            IsPrivate = request.IsPrivate,
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
public class UpdateRepositoryCommand : IRequest<bool>
{
    public int RepositoryId { get; set; }
    public int UserId { get; set; }
    public string? Description { get; set; }
    public bool? IsPrivate { get; set; }
}

/// <summary>
/// Handler for updating a repository
/// </summary>
public class UpdateRepositoryCommandHandler : IRequestHandler<UpdateRepositoryCommand, bool>
{
    private readonly ICodeRepository _codeRepository;

    public UpdateRepositoryCommandHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<bool> Handle(UpdateRepositoryCommand request, CancellationToken cancellationToken)
    {
        var repository = await _codeRepository.GetByIdAsync(request.RepositoryId, cancellationToken);
        if (repository == null || repository.OwnerId != request.UserId)
            return false;

        if (request.Description != null)
            repository.Description = request.Description;
        if (request.IsPrivate.HasValue)
            repository.IsPrivate = request.IsPrivate.Value;

        repository.LastUpdatedDate = DateTime.UtcNow;

        await _codeRepository.UpdateAsync(repository, cancellationToken);
        return true;
    }
}

/// <summary>
/// Command for deleting a repository
/// </summary>
public class DeleteRepositoryCommand : IRequest<bool>
{
    public int RepositoryId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Handler for deleting a repository
/// </summary>
public class DeleteRepositoryCommandHandler : IRequestHandler<DeleteRepositoryCommand, bool>
{
    private readonly ICodeRepository _codeRepository;

    public DeleteRepositoryCommandHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<bool> Handle(DeleteRepositoryCommand request, CancellationToken cancellationToken)
    {
        var repository = await _codeRepository.GetByIdAsync(request.RepositoryId, cancellationToken);
        if (repository == null || repository.OwnerId != request.UserId)
            return false;

        await _codeRepository.DeleteAsync(request.RepositoryId, cancellationToken);
        return true;
    }
}
