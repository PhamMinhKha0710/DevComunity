using MediatR;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Votes;

namespace SocialTechsy.SocialNetwork.Application.Commands.Votes;

/// <summary>
/// Command to vote on a question
/// </summary>
public class VoteQuestionCommand : IRequest<VoteResult>
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public string VoteType { get; set; } = null!; // "up" or "down"
}

/// <summary>
/// Command to vote on an answer
/// </summary>
public class VoteAnswerCommand : IRequest<VoteResult>
{
    public int AnswerId { get; set; }
    public int UserId { get; set; }
    public string VoteType { get; set; } = null!; // "up" or "down"
}

/// <summary>
/// Command to remove a vote
/// </summary>
public class RemoveVoteCommand : IRequest<VoteResult>
{
    public int? QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public int UserId { get; set; }
}
