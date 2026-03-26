using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Comments;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Comments;

public class CreateQuestionCommentCommandHandler : IRequestHandler<CreateQuestionCommentCommand, CommentDto?>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IQuestionEventDispatcher _dispatcher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQuestionCommentCommandHandler(
        ICommentRepository commentRepository,
        IQuestionRepository questionRepository,
        IUserRepository userRepository,
        IQuestionEventDispatcher dispatcher,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _dispatcher = dispatcher;
        _unitOfWork = unitOfWork;
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

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        var result = new CommentDto
        {
            CommentId = comment.CommentId,
            Body = comment.Body,
            CreatedDate = comment.CreatedDate,
            UserId = comment.UserId,
            AuthorId = comment.UserId,
            AuthorUsername = user?.Username ?? "",
            AuthorProfilePicture = user?.ProfilePicture
        };

        await _dispatcher.NotifyNewCommentAsync(request.QuestionId, "question", request.QuestionId, result, cancellationToken);

        return result;
    }
}

public class CreateAnswerCommentCommandHandler : IRequestHandler<CreateAnswerCommentCommand, CommentDto?>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IQuestionEventDispatcher _dispatcher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAnswerCommentCommandHandler(
        ICommentRepository commentRepository,
        IAnswerRepository answerRepository,
        IUserRepository userRepository,
        IQuestionEventDispatcher dispatcher,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _answerRepository = answerRepository;
        _userRepository = userRepository;
        _dispatcher = dispatcher;
        _unitOfWork = unitOfWork;
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

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        var result = new CommentDto
        {
            CommentId = comment.CommentId,
            Body = comment.Body,
            CreatedDate = comment.CreatedDate,
            UserId = comment.UserId,
            AuthorId = comment.UserId,
            AuthorUsername = user?.Username ?? "",
            AuthorProfilePicture = user?.ProfilePicture
        };

        await _dispatcher.NotifyNewCommentAsync(answer.QuestionId, "answer", answer.AnswerId, result, cancellationToken);

        return result;
    }
}

public class CreatePostCommentCommandHandler : IRequestHandler<CreatePostCommentCommand, CommentDto?>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePostCommentCommandHandler(
        ICommentRepository commentRepository,
        IPostRepository postRepository,
        IUserRepository userRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _userRepository = userRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommentDto?> Handle(CreatePostCommentCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null) return null;

        var comment = new Comment
        {
            PostId = request.PostId,
            UserId = request.UserId,
            Body = request.Body,
            CreatedDate = DateTime.UtcNow
        };

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove($"comments:post:{request.PostId}");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        return new CommentDto
        {
            CommentId = comment.CommentId,
            Body = comment.Body,
            CreatedDate = comment.CreatedDate,
            UserId = comment.UserId,
            AuthorId = comment.UserId,
            AuthorUsername = user?.Username ?? "Unknown",
            AuthorProfilePicture = user?.ProfilePicture
        };
    }
}

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, bool>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCommentCommandHandler(
        ICommentRepository commentRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment == null || comment.UserId != request.UserId)
            return false;

        comment.Body = request.Body;
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (comment.PostId.HasValue)
            _cacheService.Remove($"comments:post:{comment.PostId.Value}");

        return true;
    }
}

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, bool>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommentCommandHandler(
        ICommentRepository commentRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment == null || comment.UserId != request.UserId)
            return false;

        var postId = comment.PostId;

        await _commentRepository.DeleteAsync(request.CommentId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (postId.HasValue)
            _cacheService.Remove($"comments:post:{postId.Value}");

        return true;
    }
}
