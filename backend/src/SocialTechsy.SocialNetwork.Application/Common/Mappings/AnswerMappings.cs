using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Common.Mappings;

public static class AnswerMappings
{
    public static AnswerDto ToDto(this Answer a)
    {
        return new AnswerDto
        {
            AnswerId = a.AnswerId,
            QuestionId = a.QuestionId,
            Body = a.Body,
            Score = a.Score,
            IsAccepted = a.IsAccepted,
            CreatedDate = DateTime.SpecifyKind(a.CreatedDate, DateTimeKind.Utc),
            UpdatedDate = a.UpdatedDate.HasValue ? DateTime.SpecifyKind(a.UpdatedDate.Value, DateTimeKind.Utc) : null,
            AuthorId = a.User?.UserId ?? a.UserId,
            AuthorUsername = a.User?.Username,
            AuthorProfilePicture = a.User?.ProfilePicture,
            AuthorReputation = a.User?.ReputationPoints,
            ParentAnswerId = a.ParentAnswerId
        };
    }
}
