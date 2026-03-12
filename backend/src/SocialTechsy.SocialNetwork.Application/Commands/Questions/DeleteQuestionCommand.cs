using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Questions;

/// <summary>
/// Command to delete a question
/// </summary>
public class DeleteQuestionCommand : IRequest<Unit>
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
}
