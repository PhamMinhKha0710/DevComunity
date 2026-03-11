using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Questions;

/// <summary>
/// Command to delete a question
/// </summary>
public class DeleteQuestionCommand : IRequest<bool>
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
}
