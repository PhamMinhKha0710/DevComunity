namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;

using SocialTechsy.SocialNetwork.Application.Common.DTOs.Tag;

public class QuestionSummaryDto
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = null!;
    public string? BodyExcerpt { get; set; }
    public int ViewCount { get; set; }
    public int Score { get; set; }
    public int AnswerCount { get; set; }
    public bool HasAcceptedAnswer { get; set; }
    public DateTime CreatedDate { get; set; }
    public string Status { get; set; } = null!;

    public int? AuthorId { get; set; }
    public string? AuthorUsername { get; set; }
    public string? AuthorProfilePicture { get; set; }
    public int? AuthorReputation { get; set; }

    public List<TagDto> Tags { get; set; } = new();
}
