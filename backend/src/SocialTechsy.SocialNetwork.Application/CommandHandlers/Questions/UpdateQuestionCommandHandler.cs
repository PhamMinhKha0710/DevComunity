using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Common.Exceptions;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for UpdateQuestionCommand
/// </summary>
public class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand, Unit>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService? _cacheService;

    public UpdateQuestionCommandHandler(
        IQuestionRepository questionRepository,
        ITagRepository tagRepository,
        IUnitOfWork unitOfWork,
        ICacheService? cacheService = null)
    {
        _questionRepository = questionRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Unit> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var question = await _questionRepository.GetByIdForUpdateAsync(request.QuestionId, ct);

            if (question == null)
                throw new EntityNotFoundException("Question", request.QuestionId);

            if (question.UserId != request.UserId)
                throw new UnauthorizedCommandException($"User {request.UserId} is not authorized to update Question {request.QuestionId}.");

            question.Update(request.Title, request.Body);

            if (request.Tags != null)
            {
                var tagNames = request.Tags
                    .Select(t => t?.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                question.QuestionTags.Clear();

                foreach (var tagName in tagNames!)
                {
                    var tag = await _tagRepository.GetOrCreateAsync(tagName!, ct);
                    question.QuestionTags.Add(new QuestionTag { TagId = tag.TagId, QuestionId = question.QuestionId });
                }
            }

            await _questionRepository.UpdateAsync(question, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _cacheService?.Remove($"question:{request.QuestionId}");
            _cacheService?.RemoveByPrefix("questions:page:");
            _cacheService?.RemoveByPrefix("search:");

            return Unit.Value;
        }, cancellationToken);
    }
}
