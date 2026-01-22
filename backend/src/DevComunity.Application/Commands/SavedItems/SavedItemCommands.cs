namespace DevComunity.Application.Commands.SavedItems;

/// <summary>
/// Command to save a question
/// </summary>
public class SaveQuestionCommand
{
    public int UserId { get; set; }
    public int QuestionId { get; set; }
}

/// <summary>
/// Command to unsave a question
/// </summary>
public class UnsaveQuestionCommand
{
    public int UserId { get; set; }
    public int QuestionId { get; set; }
}

/// <summary>
/// Command to save an answer
/// </summary>
public class SaveAnswerCommand
{
    public int UserId { get; set; }
    public int AnswerId { get; set; }
}

/// <summary>
/// Command to unsave an answer
/// </summary>
public class UnsaveAnswerCommand
{
    public int UserId { get; set; }
    public int AnswerId { get; set; }
}
