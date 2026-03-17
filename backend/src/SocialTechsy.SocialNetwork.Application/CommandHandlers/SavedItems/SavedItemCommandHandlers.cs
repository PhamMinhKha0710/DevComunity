using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.SavedItems;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.SavedItems;

/// <summary>
/// Handler for saving a question
/// </summary>
public class SaveQuestionCommandHandler : IRequestHandler<SaveQuestionCommand, bool>
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IQuestionRepository _questionRepository;

    public SaveQuestionCommandHandler(
        ISavedItemRepository savedItemRepository,
        IQuestionRepository questionRepository)
    {
        _savedItemRepository = savedItemRepository;
        _questionRepository = questionRepository;
    }

    public async Task<bool> Handle(SaveQuestionCommand request, CancellationToken cancellationToken)
    {
        // Check if question exists
        if (!await _questionRepository.ExistsAsync(request.QuestionId, cancellationToken))
            return false;

        // Check if already saved
        if (await _savedItemRepository.IsSavedAsync(request.UserId, request.QuestionId, null, null, cancellationToken))
            return true; // Already saved

        var savedItem = new SavedItem
        {
            UserId = request.UserId,
            QuestionId = request.QuestionId,
            CreatedDate = DateTime.UtcNow
        };

        await _savedItemRepository.AddAsync(savedItem, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for unsaving a question
/// </summary>
public class UnsaveQuestionCommandHandler : IRequestHandler<UnsaveQuestionCommand>
{
    private readonly ISavedItemRepository _savedItemRepository;

    public UnsaveQuestionCommandHandler(ISavedItemRepository savedItemRepository)
    {
        _savedItemRepository = savedItemRepository;
    }

    public async Task Handle(UnsaveQuestionCommand request, CancellationToken cancellationToken)
    {
        await _savedItemRepository.DeleteByQuestionAsync(request.UserId, request.QuestionId, cancellationToken);
    }
}

/// <summary>
/// Handler for saving an answer
/// </summary>
public class SaveAnswerCommandHandler : IRequestHandler<SaveAnswerCommand, bool>
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IAnswerRepository _answerRepository;

    public SaveAnswerCommandHandler(
        ISavedItemRepository savedItemRepository,
        IAnswerRepository answerRepository)
    {
        _savedItemRepository = savedItemRepository;
        _answerRepository = answerRepository;
    }

    public async Task<bool> Handle(SaveAnswerCommand request, CancellationToken cancellationToken)
    {
        // Check if answer exists
        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);
        if (answer == null)
            return false;

        // Check if already saved
        if (await _savedItemRepository.IsSavedAsync(request.UserId, null, request.AnswerId, null, cancellationToken))
            return true; // Already saved

        var savedItem = new SavedItem
        {
            UserId = request.UserId,
            AnswerId = request.AnswerId,
            CreatedDate = DateTime.UtcNow
        };

        await _savedItemRepository.AddAsync(savedItem, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for unsaving an answer
/// </summary>
public class UnsaveAnswerCommandHandler : IRequestHandler<UnsaveAnswerCommand>
{
    private readonly ISavedItemRepository _savedItemRepository;

    public UnsaveAnswerCommandHandler(ISavedItemRepository savedItemRepository)
    {
        _savedItemRepository = savedItemRepository;
    }

    public async Task Handle(UnsaveAnswerCommand request, CancellationToken cancellationToken)
    {
        await _savedItemRepository.DeleteByAnswerAsync(request.UserId, request.AnswerId, cancellationToken);
    }
}

/// <summary>
/// Handler for saving a post
/// </summary>
public class SavePostCommandHandler : IRequestHandler<SavePostCommand, bool>
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IPostRepository _postRepository;

    public SavePostCommandHandler(
        ISavedItemRepository savedItemRepository,
        IPostRepository postRepository)
    {
        _savedItemRepository = savedItemRepository;
        _postRepository = postRepository;
    }

    public async Task<bool> Handle(SavePostCommand request, CancellationToken cancellationToken)
    {
        // Check if post exists
        var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null)
            return false;

        // Check if already saved
        if (await _savedItemRepository.IsSavedAsync(request.UserId, null, null, request.PostId, cancellationToken))
            return true; // Already saved

        var savedItem = new SavedItem
        {
            UserId = request.UserId,
            PostId = request.PostId,
            CreatedDate = DateTime.UtcNow
        };

        await _savedItemRepository.AddAsync(savedItem, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for unsaving a post
/// </summary>
public class UnsavePostCommandHandler : IRequestHandler<UnsavePostCommand>
{
    private readonly ISavedItemRepository _savedItemRepository;

    public UnsavePostCommandHandler(ISavedItemRepository savedItemRepository)
    {
        _savedItemRepository = savedItemRepository;
    }

    public async Task Handle(UnsavePostCommand request, CancellationToken cancellationToken)
    {
        await _savedItemRepository.DeleteByPostAsync(request.UserId, request.PostId, cancellationToken);
    }
}
