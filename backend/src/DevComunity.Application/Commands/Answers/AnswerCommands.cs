using System.ComponentModel.DataAnnotations;

namespace DevComunity.Application.Commands.Answers;

/// <summary>
/// Command to create a new answer
/// </summary>
public class CreateAnswerCommand
{
    public int QuestionId { get; set; }
    
    [Required]
    [MinLength(30)]
    public string Body { get; set; } = null!;
    
    public int? ParentAnswerId { get; set; }
    
    public int UserId { get; set; }
}

/// <summary>
/// Command to update an existing answer
/// </summary>
public class UpdateAnswerCommand
{
    public int AnswerId { get; set; }
    
    [Required]
    [MinLength(30)]
    public string Body { get; set; } = null!;
    
    public int UserId { get; set; }
}

/// <summary>
/// Command to delete an answer
/// </summary>
public class DeleteAnswerCommand
{
    public int AnswerId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command to accept an answer
/// </summary>
public class AcceptAnswerCommand
{
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }
    public int UserId { get; set; } // Question author
}

/// <summary>
/// Command to vote on an answer
/// </summary>
public class VoteAnswerCommand
{
    public int AnswerId { get; set; }
    public int UserId { get; set; }
    public string VoteType { get; set; } = null!; // "up", "down", "remove"
}
