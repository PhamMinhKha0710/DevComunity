using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Questions;

/// <summary>
/// Command to update an existing question
/// </summary>
public class UpdateQuestionCommand : IRequest<Unit>
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public int UserId { get; set; }
}
