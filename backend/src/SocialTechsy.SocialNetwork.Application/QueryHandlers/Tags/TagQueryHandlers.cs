using MediatR;
using SocialTechsy.SocialNetwork.Application.Queries.Tags;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Tags;

public class GetTagsQueryHandler : IRequestHandler<GetTagsQuery, PaginatedResponse<TagDto>>
{
    private readonly ITagRepository _tagRepository;
    private readonly ICacheService _cacheService;

    public GetTagsQueryHandler(ITagRepository tagRepository, ICacheService cacheService)
    {
        _tagRepository = tagRepository;
        _cacheService = cacheService;
    }

    public async Task<PaginatedResponse<TagDto>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"tags_{request.Page}_{request.PageSize}_{request.Search ?? ""}_{request.Sort ?? ""}";

        var result = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            var (items, totalCount) = await _tagRepository.GetPaginatedAsync(
                request.Page, request.PageSize, request.Search, request.Sort, cancellationToken);

            return new PaginatedResponse<TagDto>
            {
                Items = items.Select(t => new TagDto
                {
                    TagId = t.TagId,
                    TagName = t.TagName,
                    Description = t.Description,
                    QuestionCount = t.QuestionTags?.Count ?? 0
                }).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }, TimeSpan.FromMinutes(10));

        return result!;
    }
}

public class GetTagByNameQueryHandler : IRequestHandler<GetTagByNameQuery, TagDto?>
{
    private readonly ITagRepository _tagRepository;
    private readonly ICacheService _cacheService;

    public GetTagByNameQueryHandler(ITagRepository tagRepository, ICacheService cacheService)
    {
        _tagRepository = tagRepository;
        _cacheService = cacheService;
    }

    public async Task<TagDto?> Handle(GetTagByNameQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"tag_{request.TagName}";

        return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            var tag = await _tagRepository.GetByNameAsync(request.TagName, cancellationToken);
            if (tag == null) return null;

            return new TagDto
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                Description = tag.Description,
                QuestionCount = tag.QuestionTags?.Count ?? 0
            };
        }, TimeSpan.FromMinutes(10));
    }
}
