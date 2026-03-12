using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Common.Mappings;

public static class UserMappings
{
    public static UserDto ToDto(this User u, int questionCount = 0, int answerCount = 0)
    {
        return new UserDto
        {
            UserId = u.UserId,
            Username = u.Username,
            Email = u.Email,
            DisplayName = u.DisplayName,
            ProfilePicture = u.ProfilePicture,
            Bio = u.Bio,
            Location = u.Location,
            Website = u.Website,
            ReputationPoints = u.ReputationPoints,
            IsEmailVerified = u.IsEmailVerified,
            QuestionCount = questionCount,
            AnswerCount = answerCount,
            CreatedDate = u.CreatedDate
        };
    }
}
