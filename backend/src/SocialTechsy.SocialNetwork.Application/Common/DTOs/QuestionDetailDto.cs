namespace SocialTechsy.SocialNetwork.Application.Common.DTOs;

public class QuestionDetailDto : QuestionSummaryDto
{
    public string Body { get; set; } = null!;
    public DateTime? UpdatedDate { get; set; }

    public string? UserVoteType { get; set; }
    public bool IsSaved { get; set; }
}
