using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Comments;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Comments;

/// <summary>
/// Handler for creating a comment on a question
/// </summary>
public class CreateQuestionCommentCommandHandler : IRequestHandler<CreateQuestionCommentCommand, CommentDto?>
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

    public async Task<CommentDto?> Handle(CreateQuestionCommentCommand request, CancellationToken cancellationToken)
    {
        var questionExists = await _questionRepository.ExistsAsync(request.QuestionId, cancellationToken);
        if (!questionExists) return null;

        var comment = new Comment
        {
            QuestionId = request.QuestionId,
            UserId = request.UserId,
            Body = request.Body,
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
public class CreateAnswerCommentCommandHandler : IRequestHandler<CreateAnswerCommentCommand, CommentDto?>
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

    public async Task<CommentDto?> Handle(CreateAnswerCommentCommand request, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);
        if (answer == null) return null;

        var comment = new Comment
        {
            AnswerId = request.AnswerId,
            UserId = request.UserId,
            Body = request.Body,
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
public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, bool>
{
    private readonly ICommentRepository _commentRepository;

    public UpdateCommentCommandHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<bool> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment == null || comment.UserId != request.UserId)
            return false;

        comment.Body = request.Body;
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for deleting a comment
/// </summary>
public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, bool>
{
    private readonly ICommentRepository _commentRepository;

    public DeleteCommentCommandHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment == null || comment.UserId != request.UserId)
            return false;

        await _commentRepository.DeleteAsync(request.CommentId, cancellationToken);
        return true;
    }
}
