using DevComunity.Application.Queries.Tags;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;

namespace DevComunity.Application.QueryHandlers.Tags;

/// <summary>
/// Handler for getting paginated tags
/// </summary>
public class GetTagsQueryHandler
{
    private readonly ITagRepository _tagRepository;

    public GetTagsQueryHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<PaginatedResponse<TagDto>> HandleAsync(GetTagsQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _tagRepository.GetPaginatedAsync(
            query.Page,
            query.PageSize,
            query.Search,
            query.Sort,
            cancellationToken);

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
    }
}

/// <summary>
/// Handler for getting a tag by name
/// </summary>
public class GetTagByNameQueryHandler
{
    private readonly ITagRepository _tagRepository;

    public GetTagByNameQueryHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<TagDto?> HandleAsync(GetTagByNameQuery query, CancellationToken cancellationToken)
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
    }
}
