using MediatR;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Answers;
using SocialTechsy.SocialNetwork.Application.Common;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

namespace SocialTechsy.SocialNetwork.Application.Commands.Answers;

/// <summary>
/// Command to create a new answer
/// </summary>
public class CreateAnswerCommand : IRequest<AnswerDto?>
{
    public int QuestionId { get; set; }
    public string Body { get; set; } = null!;
    public int? ParentAnswerId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command to update an existing answer
/// </summary>
public class UpdateAnswerCommand : IRequest<bool>
{
    public int AnswerId { get; set; }
    public string Body { get; set; } = null!;
    public int UserId { get; set; }
}

/// <summary>
/// Command to delete an answer
/// </summary>
public class DeleteAnswerCommand : IRequest<bool>
{
    public int AnswerId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command to accept an answer
/// </summary>
public class AcceptAnswerCommand : IRequest<AcceptAnswerResult>, ITransactionalRequest
{
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }
    public int UserId { get; set; } // Question author
}
