using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Common.Mappings;

public static class QuestionMappings
{
    public static QuestionSummaryDto ToSummaryDto(this Question q)
    {
        return new QuestionSummaryDto
        {
            QuestionId = q.QuestionId,
            Title = q.Title,
            BodyExcerpt = q.Body.Length > 200 ? q.Body[..200] + "..." : q.Body,
            ViewCount = q.ViewCount,
            Score = q.Score,
            AnswerCount = q.Answers?.Count ?? 0,
            HasAcceptedAnswer = q.Answers?.Any(a => a.IsAccepted) ?? false,
            CreatedDate = DateTime.SpecifyKind(q.CreatedDate, DateTimeKind.Utc),
            Status = q.Status ?? "open",
            AuthorId = q.User?.UserId ?? q.UserId,
            AuthorUsername = q.User?.Username,
            AuthorProfilePicture = q.User?.ProfilePicture,
            AuthorReputation = q.User?.ReputationPoints,
            Tags = q.QuestionTags?.Select(qt => new TagDto
            {
                TagId = qt.Tag?.TagId ?? 0,
                TagName = qt.Tag?.TagName ?? "",
                Description = qt.Tag?.Description
            }).ToList() ?? new List<TagDto>()
        };
    }

    public static QuestionDetailDto ToDetailDto(this Question q)
    {
        return new QuestionDetailDto
        {
            QuestionId = q.QuestionId,
            Title = q.Title,
            Body = q.Body,
            BodyExcerpt = q.Body.Length > 200 ? q.Body[..200] + "..." : q.Body,
            ViewCount = q.ViewCount,
            Score = q.Score,
            AnswerCount = q.Answers?.Count ?? 0,
            HasAcceptedAnswer = q.Answers?.Any(a => a.IsAccepted) ?? false,
            CreatedDate = DateTime.SpecifyKind(q.CreatedDate, DateTimeKind.Utc),
            UpdatedDate = q.UpdatedDate.HasValue ? DateTime.SpecifyKind(q.UpdatedDate.Value, DateTimeKind.Utc) : null,
            Status = q.Status ?? "open",
            AuthorId = q.User?.UserId ?? q.UserId,
            AuthorUsername = q.User?.Username,
            AuthorProfilePicture = q.User?.ProfilePicture,
            AuthorReputation = q.User?.ReputationPoints,
            Tags = q.QuestionTags?.Select(qt => new TagDto
            {
                TagId = qt.Tag?.TagId ?? 0,
                TagName = qt.Tag?.TagName ?? "",
                Description = qt.Tag?.Description
            }).ToList() ?? new List<TagDto>()
        };
    }
}
