using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Tag;
using SocialTechsy.SocialNetwork.Application.Commands.Tags;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Tags;

public class FollowTagCommandHandler : IRequestHandler<FollowTagCommand, TagPreferenceDto>
{
    private readonly ITagRepository _tagRepository;
    private readonly ITagPreferenceRepository _tagPreferenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FollowTagCommandHandler(
        ITagRepository tagRepository,
        ITagPreferenceRepository tagPreferenceRepository,
        IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _tagPreferenceRepository = tagPreferenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TagPreferenceDto> Handle(FollowTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdAsync(request.TagId, cancellationToken);
        if (tag == null)
            throw new InvalidOperationException($"Tag with ID {request.TagId} not found");

        var preference = new TagPreference
        {
            UserId = request.UserId,
            TagId = request.TagId,
            IsFollowed = true,
            IsIgnored = false
        };

        await _tagPreferenceRepository.UpsertAsync(preference, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TagPreferenceDto
        {
            TagId = tag.TagId,
            TagName = tag.TagName,
            IsFollowed = true,
            IsIgnored = false
        };
    }
}

public class IgnoreTagCommandHandler : IRequestHandler<IgnoreTagCommand, TagPreferenceDto>
{
    private readonly ITagRepository _tagRepository;
    private readonly ITagPreferenceRepository _tagPreferenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IgnoreTagCommandHandler(
        ITagRepository tagRepository,
        ITagPreferenceRepository tagPreferenceRepository,
        IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _tagPreferenceRepository = tagPreferenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TagPreferenceDto> Handle(IgnoreTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdAsync(request.TagId, cancellationToken);
        if (tag == null)
            throw new InvalidOperationException($"Tag with ID {request.TagId} not found");

        var preference = new TagPreference
        {
            UserId = request.UserId,
            TagId = request.TagId,
            IsFollowed = false,
            IsIgnored = true
        };

        await _tagPreferenceRepository.UpsertAsync(preference, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TagPreferenceDto
        {
            TagId = tag.TagId,
            TagName = tag.TagName,
            IsFollowed = false,
            IsIgnored = true
        };
    }
}

public class RemoveTagPreferenceCommandHandler : IRequestHandler<RemoveTagPreferenceCommand, bool>
{
    private readonly ITagPreferenceRepository _tagPreferenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveTagPreferenceCommandHandler(ITagPreferenceRepository tagPreferenceRepository, IUnitOfWork unitOfWork)
    {
        _tagPreferenceRepository = tagPreferenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RemoveTagPreferenceCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _tagPreferenceRepository.DeleteAsync(request.UserId, request.TagId, cancellationToken);
        if (deleted)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        return deleted;
    }
}
