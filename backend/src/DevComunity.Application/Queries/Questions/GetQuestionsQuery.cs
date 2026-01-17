namespace DevComunity.Application.Queries.Questions;

/// <summary>
/// Query to get paginated list of questions
/// </summary>
public class GetQuestionsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public string? SearchTerm { get; set; }
    public string? Tag { get; set; }
    public string Sort { get; set; } = "newest"; // newest, active, votes, unanswered
    public int? CurrentUserId { get; set; }
}
