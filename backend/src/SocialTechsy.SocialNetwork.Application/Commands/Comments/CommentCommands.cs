using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Commands.Comments;

/// <summary>
/// Command to create a comment on a question
/// </summary>
public class CreateQuestionCommentCommand : IRequest<CommentDto?>
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public string Body { get; set; } = null!;
}

/// <summary>
/// Command to create a comment on an answer
/// </summary>
public class CreateAnswerCommentCommand : IRequest<CommentDto?>
{
    public int AnswerId { get; set; }
    public int UserId { get; set; }
    public string Body { get; set; } = null!;
}

/// <summary>
/// Command to update a comment
/// </summary>
public class UpdateCommentCommand : IRequest<bool>
{
    public int CommentId { get; set; }
    public int UserId { get; set; }
    public string Body { get; set; } = null!;
}

/// <summary>
/// Command to delete a comment
/// </summary>
public class DeleteCommentCommand : IRequest<bool>
{
    public int CommentId { get; set; }
    public int UserId { get; set; }
}
