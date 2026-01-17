namespace DevComunity.Application.Commands.Votes;

/// <summary>
/// Command to vote on a question
/// </summary>
public class VoteQuestionCommand
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public string VoteType { get; set; } = null!; // "up" or "down"
}

/// <summary>
/// Command to vote on an answer
/// </summary>
public class VoteAnswerCommand
{
    public int AnswerId { get; set; }
    public int UserId { get; set; }
    public string VoteType { get; set; } = null!; // "up" or "down"
}

/// <summary>
/// Command to remove a vote
/// </summary>
public class RemoveVoteCommand
{
    public int? QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public int UserId { get; set; }
}
