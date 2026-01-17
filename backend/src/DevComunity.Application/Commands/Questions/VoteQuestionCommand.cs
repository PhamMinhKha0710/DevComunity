namespace DevComunity.Application.Commands.Questions;

/// <summary>
/// Command to vote on a question
/// </summary>
public class VoteQuestionCommand
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public string VoteType { get; set; } = null!; // "up", "down", "remove"
}
