namespace DevComunity.Application.Queries.Answers;

/// <summary>
/// Query to get answers for a question
/// </summary>
public class GetAnswersByQuestionQuery
{
    public int QuestionId { get; set; }
    public int? CurrentUserId { get; set; }
    public string Sort { get; set; } = "votes"; // votes, oldest, newest
}

/// <summary>
/// Query to get a single answer by ID
/// </summary>
public class GetAnswerByIdQuery
{
    public int AnswerId { get; set; }
    public int? CurrentUserId { get; set; }
}
