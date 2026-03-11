using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.SavedItems;

/// <summary>
/// Command to save a question
/// </summary>
public class SaveQuestionCommand : IRequest<bool>
{
    public int UserId { get; set; }
    public int QuestionId { get; set; }
}

/// <summary>
/// Command to unsave a question
/// </summary>
public class UnsaveQuestionCommand : IRequest
{
    public int UserId { get; set; }
    public int QuestionId { get; set; }
}

/// <summary>
/// Command to save an answer
/// </summary>
public class SaveAnswerCommand : IRequest<bool>
{
    public int UserId { get; set; }
    public int AnswerId { get; set; }
}

/// <summary>
/// Command to unsave an answer
/// </summary>
public class UnsaveAnswerCommand : IRequest
{
    public int UserId { get; set; }
    public int AnswerId { get; set; }
}
