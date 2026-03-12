using MediatR;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Votes;
using SocialTechsy.SocialNetwork.Application.Common;
using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Application.Commands.Votes;

/// <summary>
/// Command to vote on a question
/// </summary>
public class VoteQuestionCommand : IRequest<VoteResult>, ITransactionalRequest
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public VoteType VoteType { get; set; }
}

/// <summary>
/// Command to vote on an answer
/// </summary>
public class VoteAnswerCommand : IRequest<VoteResult>, ITransactionalRequest
{
    public int AnswerId { get; set; }
    public int UserId { get; set; }
    public VoteType VoteType { get; set; }
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
