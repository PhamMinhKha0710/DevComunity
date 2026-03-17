using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Common.Exceptions;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for DeleteQuestionCommand
/// </summary>
public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand, Unit>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ICacheService? _cacheService;

    public DeleteQuestionCommandHandler(
        IQuestionRepository questionRepository,
        ICacheService? cacheService = null)
    {
        _questionRepository = questionRepository;
        _cacheService = cacheService;
    }

    public async Task<Unit> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);

        if (question == null)
            throw new EntityNotFoundException("Question", request.QuestionId);

        if (question.UserId != request.UserId)
            throw new UnauthorizedCommandException($"User {request.UserId} is not authorized to delete Question {request.QuestionId}.");

        await _questionRepository.DeleteAsync(request.QuestionId, cancellationToken);

        // Invalidate cache
        _cacheService?.Remove($"question:{request.QuestionId}");
        _cacheService?.RemoveByPrefix("questions:page:");
        _cacheService?.RemoveByPrefix("search:");

        return Unit.Value;
    }
}
