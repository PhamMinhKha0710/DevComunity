using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Tags;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Tags;

public class GetUserTagPreferencesQueryHandler : IRequestHandler<GetUserTagPreferencesQuery, IEnumerable<TagPreferenceDto>>
{
    private readonly ITagPreferenceRepository _tagPreferenceRepository;

    public GetUserTagPreferencesQueryHandler(ITagPreferenceRepository tagPreferenceRepository)
    {
        _tagPreferenceRepository = tagPreferenceRepository;
    }

    public async Task<IEnumerable<TagPreferenceDto>> Handle(
        GetUserTagPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var preferences = await _tagPreferenceRepository.GetUserPreferencesAsync(request.UserId, cancellationToken);

        return preferences.Select(p => new TagPreferenceDto
        {
            TagId = p.TagId,
            TagName = p.Tag?.TagName ?? "",
            IsFollowed = p.IsFollowed,
            IsIgnored = p.IsIgnored
        });
    }
}

public class GetFollowedTagsQueryHandler : IRequestHandler<GetFollowedTagsQuery, IEnumerable<TagPreferenceDto>>
{
    private readonly ITagPreferenceRepository _tagPreferenceRepository;

    public GetFollowedTagsQueryHandler(ITagPreferenceRepository tagPreferenceRepository)
    {
        _tagPreferenceRepository = tagPreferenceRepository;
    }

    public async Task<IEnumerable<TagPreferenceDto>> Handle(
        GetFollowedTagsQuery request,
        CancellationToken cancellationToken)
    {
        var preferences = await _tagPreferenceRepository.GetFollowedTagsAsync(request.UserId, cancellationToken);

        return preferences.Select(p => new TagPreferenceDto
        {
            TagId = p.TagId,
            TagName = p.Tag?.TagName ?? "",
            IsFollowed = true,
            IsIgnored = false
        });
    }
}
