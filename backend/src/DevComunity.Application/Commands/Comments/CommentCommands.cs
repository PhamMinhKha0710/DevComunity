namespace DevComunity.Application.Commands.Comments;

/// <summary>
/// Command to create a comment on a question
/// </summary>
public class CreateQuestionCommentCommand
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public string Body { get; set; } = null!;
}

/// <summary>
/// Command to create a comment on an answer
/// </summary>
public class CreateAnswerCommentCommand
{
    public int AnswerId { get; set; }
    public int UserId { get; set; }
    public string Body { get; set; } = null!;
}

/// <summary>
/// Command to update a comment
/// </summary>
public class UpdateCommentCommand
{
    public int CommentId { get; set; }
    public int UserId { get; set; }
    public string Body { get; set; } = null!;
}

/// <summary>
/// Command to delete a comment
/// </summary>
public class DeleteCommentCommand
{
    public int CommentId { get; set; }
    public int UserId { get; set; }
}
