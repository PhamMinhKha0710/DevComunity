using DevComunity.Application.Commands.Comments;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;

namespace DevComunity.Application.CommandHandlers.Comments;

/// <summary>
/// Handler for creating a comment on a question
/// </summary>
public class CreateQuestionCommentCommandHandler
{
    private readonly ICommentRepository _commentRepository;
    private readonly IQuestionRepository _questionRepository;

    public CreateQuestionCommentCommandHandler(
        ICommentRepository commentRepository,
        IQuestionRepository questionRepository)
    {
        _commentRepository = commentRepository;
        _questionRepository = questionRepository;
    }

    public async Task<CommentDto?> HandleAsync(CreateQuestionCommentCommand command, CancellationToken cancellationToken)
    {
        var questionExists = await _questionRepository.ExistsAsync(command.QuestionId, cancellationToken);
        if (!questionExists) return null;

        var comment = new Comment
        {
            QuestionId = command.QuestionId,
            UserId = command.UserId,
            Body = command.Body,
            CreatedDate = DateTime.UtcNow
        };

        var created = await _commentRepository.AddAsync(comment, cancellationToken);
        
        return new CommentDto
        {
            CommentId = created.CommentId,
            Body = created.Body,
            CreatedDate = created.CreatedDate,
            UserId = created.UserId
        };
    }
}

/// <summary>
/// Handler for creating a comment on an answer
/// </summary>
public class CreateAnswerCommentCommandHandler
{
    private readonly ICommentRepository _commentRepository;
    private readonly IAnswerRepository _answerRepository;

    public CreateAnswerCommentCommandHandler(
        ICommentRepository commentRepository,
        IAnswerRepository answerRepository)
    {
        _commentRepository = commentRepository;
        _answerRepository = answerRepository;
    }

    public async Task<CommentDto?> HandleAsync(CreateAnswerCommentCommand command, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(command.AnswerId, cancellationToken);
        if (answer == null) return null;

        var comment = new Comment
        {
            AnswerId = command.AnswerId,
            UserId = command.UserId,
            Body = command.Body,
            CreatedDate = DateTime.UtcNow
        };

        var created = await _commentRepository.AddAsync(comment, cancellationToken);
        
        return new CommentDto
        {
            CommentId = created.CommentId,
            Body = created.Body,
            CreatedDate = created.CreatedDate,
            UserId = created.UserId
        };
    }
}

/// <summary>
/// Handler for updating a comment
/// </summary>
public class UpdateCommentCommandHandler
{
    private readonly ICommentRepository _commentRepository;

    public UpdateCommentCommandHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<bool> HandleAsync(UpdateCommentCommand command, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(command.CommentId, cancellationToken);
        if (comment == null || comment.UserId != command.UserId)
            return false;

        comment.Body = command.Body;
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for deleting a comment
/// </summary>
public class DeleteCommentCommandHandler
{
    private readonly ICommentRepository _commentRepository;

    public DeleteCommentCommandHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<bool> HandleAsync(DeleteCommentCommand command, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(command.CommentId, cancellationToken);
        if (comment == null || comment.UserId != command.UserId)
            return false;

        await _commentRepository.DeleteAsync(command.CommentId, cancellationToken);
        return true;
    }
}
