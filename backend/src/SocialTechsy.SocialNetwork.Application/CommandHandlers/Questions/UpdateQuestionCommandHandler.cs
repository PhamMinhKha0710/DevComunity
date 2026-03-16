using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Common.Exceptions;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for UpdateQuestionCommand
/// </summary>
public class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand, Unit>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ICacheService? _cacheService;

    public UpdateQuestionCommandHandler(
        IQuestionRepository questionRepository,
        ICacheService? cacheService = null)
    {
        _questionRepository = questionRepository;
        _cacheService = cacheService;
    }

    public async Task<Unit> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);

        if (question == null)
            throw new EntityNotFoundException("Question", request.QuestionId);

        if (question.UserId != request.UserId)
            throw new UnauthorizedCommandException($"User {request.UserId} is not authorized to update Question {request.QuestionId}.");

        question.Title = request.Title;
        question.Body = request.Body;
        question.UpdatedDate = DateTime.UtcNow;

        await _questionRepository.UpdateAsync(question, cancellationToken);

        if (request.Tags != null)
            await _questionRepository.SetTagsForQuestionAsync(request.QuestionId, request.Tags, cancellationToken);

        // Invalidate cache
        _cacheService?.Remove($"question:{request.QuestionId}");
        _cacheService?.RemoveByPrefix("questions:page:");
        _cacheService?.RemoveByPrefix("search:");

        return Unit.Value;
    }
}
