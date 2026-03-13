using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Questions;

/// <summary>
/// Command to increment view count for a question (fire-and-forget friendly)
/// </summary>
public class IncrementViewCountCommand : IRequest
{
    public int QuestionId { get; set; }
}
