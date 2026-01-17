namespace DevComunity.Application.Common.DTOs;

/// <summary>
/// DTO for answer response
/// </summary>
public class AnswerDto
{
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }
    public string Body { get; set; } = null!;
    public int Score { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    
    // Author info
    public int? AuthorId { get; set; }
    public string? AuthorUsername { get; set; }
    public string? AuthorProfilePicture { get; set; }
    public int? AuthorReputation { get; set; }
    
    // Reply chain
    public int? ParentAnswerId { get; set; }
    public List<AnswerDto> ChildAnswers { get; set; } = new();
    
    // Comments
    public List<CommentDto> Comments { get; set; } = new();
    
    // Current user state
    public string? UserVoteType { get; set; }
    public bool IsSaved { get; set; }
}
