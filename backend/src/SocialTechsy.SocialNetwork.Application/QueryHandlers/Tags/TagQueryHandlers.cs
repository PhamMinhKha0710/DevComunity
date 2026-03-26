using System.Text;
using MediatR;
using SocialTechsy.SocialNetwork.Application.Queries.Tags;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Tag;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Tags;

static class EncodingHelper
{
    /// <summary>
    /// Fixes text that was stored as UTF-8 bytes in an NVARCHAR(Latin1) column,
    /// then read by EF Core as Latin-1 and converted to C# UTF-16 string.
    /// Reverses the double-encoding: Latin-1 bytes -> UTF-8 -> UTF-16 (C# string).
    /// </summary>
    public static string FixUtf8Encoding(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;
        try
        {
            byte[] rawBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(input);
            return Encoding.UTF8.GetString(rawBytes);
        }
        catch { return input; }
    }
}

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
                request.Page, request.PageSize, request.Search, request.Sort ?? "popular", cancellationToken);

            var list = items.ToList();
            var counts = await _tagRepository.GetQuestionCountsByTagIdsAsync(
                list.Select(t => t.TagId),
                cancellationToken);

            return new PaginatedResponse<TagDto>
            {
                Items = list.Select(t => new TagDto
                {
                    TagId = t.TagId,
                    TagName = EncodingHelper.FixUtf8Encoding(t.TagName),
                    Description = EncodingHelper.FixUtf8Encoding(t.Description),
                    QuestionCount = counts.GetValueOrDefault(t.TagId)
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

            var counts = await _tagRepository.GetQuestionCountsByTagIdsAsync(new[] { tag.TagId }, cancellationToken);

            return new TagDto
            {
                TagId = tag.TagId,
                TagName = EncodingHelper.FixUtf8Encoding(tag.TagName),
                Description = EncodingHelper.FixUtf8Encoding(tag.Description),
                QuestionCount = counts.GetValueOrDefault(tag.TagId)
            };
        }, TimeSpan.FromMinutes(10));
    }
}
