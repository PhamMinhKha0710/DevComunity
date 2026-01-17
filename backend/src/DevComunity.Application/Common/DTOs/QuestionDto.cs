namespace DevComunity.Application.Common.DTOs;

/// <summary>
/// DTO for question list response
/// </summary>
public class QuestionDto
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? BodyExcerpt { get; set; }
    public int ViewCount { get; set; }
    public int Score { get; set; }
    public int AnswerCount { get; set; }
    public bool HasAcceptedAnswer { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string Status { get; set; } = null!;
    
    // Author info
    public int? AuthorId { get; set; }
    public string? AuthorUsername { get; set; }
    public string? AuthorProfilePicture { get; set; }
    public int? AuthorReputation { get; set; }
    
    // Tags
    public List<TagDto> Tags { get; set; } = new();
    
    // Current user state (populated if authenticated)
    public string? UserVoteType { get; set; }
    public bool IsSaved { get; set; }
}
