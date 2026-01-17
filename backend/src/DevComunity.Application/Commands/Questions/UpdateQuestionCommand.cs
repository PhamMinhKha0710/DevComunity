using System.ComponentModel.DataAnnotations;

namespace DevComunity.Application.Commands.Questions;

/// <summary>
/// Command to update an existing question
/// </summary>
public class UpdateQuestionCommand
{
    public int QuestionId { get; set; }
    
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Title { get; set; } = null!;

    [Required]
    [MinLength(30)]
    public string Body { get; set; } = null!;

    public List<string> Tags { get; set; } = new();
    
    public int UserId { get; set; }
}
