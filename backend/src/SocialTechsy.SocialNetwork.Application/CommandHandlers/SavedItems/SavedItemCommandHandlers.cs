using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.SavedItems;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.SavedItems;

public class SaveQuestionCommandHandler : IRequestHandler<SaveQuestionCommand, bool>
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveQuestionCommandHandler(
        ISavedItemRepository savedItemRepository,
        IQuestionRepository questionRepository,
        IUnitOfWork unitOfWork)
    {
        _savedItemRepository = savedItemRepository;
        _questionRepository = questionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(SaveQuestionCommand request, CancellationToken cancellationToken)
    {
        if (!await _questionRepository.ExistsAsync(request.QuestionId, cancellationToken))
            return false;

        if (await _savedItemRepository.IsSavedAsync(request.UserId, request.QuestionId, null, null, cancellationToken))
            return true;

        var savedItem = new SavedItem
        {
            UserId = request.UserId,
            QuestionId = request.QuestionId,
            CreatedDate = DateTime.UtcNow
        };

        await _savedItemRepository.AddAsync(savedItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class UnsaveQuestionCommandHandler : IRequestHandler<UnsaveQuestionCommand>
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnsaveQuestionCommandHandler(ISavedItemRepository savedItemRepository, IUnitOfWork unitOfWork)
    {
        _savedItemRepository = savedItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnsaveQuestionCommand request, CancellationToken cancellationToken)
    {
        await _savedItemRepository.DeleteByQuestionAsync(request.UserId, request.QuestionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class SaveAnswerCommandHandler : IRequestHandler<SaveAnswerCommand, bool>
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveAnswerCommandHandler(
        ISavedItemRepository savedItemRepository,
        IAnswerRepository answerRepository,
        IUnitOfWork unitOfWork)
    {
        _savedItemRepository = savedItemRepository;
        _answerRepository = answerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(SaveAnswerCommand request, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);
        if (answer == null)
            return false;

        if (await _savedItemRepository.IsSavedAsync(request.UserId, null, request.AnswerId, null, cancellationToken))
            return true;

        var savedItem = new SavedItem
        {
            UserId = request.UserId,
            AnswerId = request.AnswerId,
            CreatedDate = DateTime.UtcNow
        };

        await _savedItemRepository.AddAsync(savedItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class UnsaveAnswerCommandHandler : IRequestHandler<UnsaveAnswerCommand>
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnsaveAnswerCommandHandler(ISavedItemRepository savedItemRepository, IUnitOfWork unitOfWork)
    {
        _savedItemRepository = savedItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnsaveAnswerCommand request, CancellationToken cancellationToken)
    {
        await _savedItemRepository.DeleteByAnswerAsync(request.UserId, request.AnswerId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class SavePostCommandHandler : IRequestHandler<SavePostCommand, bool>
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IPostRepository _postRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SavePostCommandHandler(
        ISavedItemRepository savedItemRepository,
        IPostRepository postRepository,
        IUnitOfWork unitOfWork)
    {
        _savedItemRepository = savedItemRepository;
        _postRepository = postRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(SavePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null)
            return false;

        if (await _savedItemRepository.IsSavedAsync(request.UserId, null, null, request.PostId, cancellationToken))
            return true;

        var savedItem = new SavedItem
        {
            UserId = request.UserId,
            PostId = request.PostId,
            CreatedDate = DateTime.UtcNow
        };

        await _savedItemRepository.AddAsync(savedItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class UnsavePostCommandHandler : IRequestHandler<UnsavePostCommand>
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnsavePostCommandHandler(ISavedItemRepository savedItemRepository, IUnitOfWork unitOfWork)
    {
        _savedItemRepository = savedItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnsavePostCommand request, CancellationToken cancellationToken)
    {
        await _savedItemRepository.DeleteByPostAsync(request.UserId, request.PostId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
