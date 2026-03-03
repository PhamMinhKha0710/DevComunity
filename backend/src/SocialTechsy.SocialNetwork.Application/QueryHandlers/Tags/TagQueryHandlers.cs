using SocialTechsy.SocialNetwork.Application.Queries.Tags;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Tags;

public class GetTagsQueryHandler
{
    private readonly ITagRepository _tagRepository;
    private readonly ICacheService _cacheService;

    public GetTagsQueryHandler(ITagRepository tagRepository, ICacheService cacheService)
    {
        _tagRepository = tagRepository;
        _cacheService = cacheService;
    }

    public async Task<PaginatedResponse<TagDto>> HandleAsync(GetTagsQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"tags_{query.Page}_{query.PageSize}_{query.Search ?? ""}_{query.Sort ?? ""}";

        var result = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            var (items, totalCount) = await _tagRepository.GetPaginatedAsync(
                query.Page, query.PageSize, query.Search, query.Sort, cancellationToken);

            return new PaginatedResponse<TagDto>
            {
                Items = items.Select(t => new TagDto
                {
                    TagId = t.TagId,
                    TagName = t.TagName,
                    Description = t.Description,
                    QuestionCount = t.QuestionTags?.Count ?? 0
                }).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }, TimeSpan.FromMinutes(10));

        return result!;
    }
}

public class GetTagByNameQueryHandler
{
    private readonly ITagRepository _tagRepository;
    private readonly ICacheService _cacheService;

    public GetTagByNameQueryHandler(ITagRepository tagRepository, ICacheService cacheService)
    {
        _tagRepository = tagRepository;
        _cacheService = cacheService;
    }

    public async Task<TagDto?> HandleAsync(GetTagByNameQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"tag_{query.TagName}";

        return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            var tag = await _tagRepository.GetByNameAsync(query.TagName, cancellationToken);
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
