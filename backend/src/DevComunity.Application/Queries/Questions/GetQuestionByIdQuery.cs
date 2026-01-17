namespace DevComunity.Application.Queries.Questions;

/// <summary>
/// Query to get a single question by ID
/// </summary>
public class GetQuestionByIdQuery
{
    public int QuestionId { get; set; }
    public int? CurrentUserId { get; set; }
}
