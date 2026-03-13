using System.ComponentModel.DataAnnotations;
using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Commands.Questions;

/// <summary>
/// Command to create a new question
/// </summary>
public class CreateQuestionCommand : IRequest<QuestionDto>
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Title { get; set; } = null!;

    [Required]
    [MinLength(30)]
    public string Body { get; set; } = null!;

    public List<string> Tags { get; set; } = new();
    
    public int UserId { get; set; }
}
