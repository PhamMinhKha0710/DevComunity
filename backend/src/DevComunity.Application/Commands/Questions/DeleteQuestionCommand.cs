namespace DevComunity.Application.Commands.Questions;

/// <summary>
/// Command to delete a question
/// </summary>
public class DeleteQuestionCommand
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
}
