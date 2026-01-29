namespace DevComunity.Application.Common.DTOs;

/// <summary>
/// DTO for saved items with question/answer details
/// </summary>
public class SavedItemDto
{
    public int SavedItemId { get; set; }
    public DateTime CreatedDate { get; set; }
    public string Type { get; set; } = null!; // "question" or "answer"
    
    // Question details (if type is "question")
    public int? QuestionId { get; set; }
    public string? QuestionTitle { get; set; }
    public int? QuestionScore { get; set; }
    public int? QuestionAnswerCount { get; set; }
    
    // Answer details (if type is "answer")
    public int? AnswerId { get; set; }
    public string? AnswerBody { get; set; }
    public int? AnswerScore { get; set; }
    public int? RelatedQuestionId { get; set; }
    public string? RelatedQuestionTitle { get; set; }
}
