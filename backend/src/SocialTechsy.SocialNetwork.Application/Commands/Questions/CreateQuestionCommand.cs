using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Commands.Questions;

/// <summary>
/// Command to create a new question
/// </summary>
public class CreateQuestionCommand : IRequest<QuestionDto>
{
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public int UserId { get; set; }
}
