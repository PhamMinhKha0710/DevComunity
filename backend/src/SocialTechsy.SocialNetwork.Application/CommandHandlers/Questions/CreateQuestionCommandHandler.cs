using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for CreateQuestionCommand
/// </summary>
public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, QuestionDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService? _cacheService;

    public CreateQuestionCommandHandler(
        IQuestionRepository questionRepository,
        ITagRepository tagRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICacheService? cacheService = null)
    {
        _questionRepository = questionRepository;
        _tagRepository = tagRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<QuestionDto> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var question = Question.Create(request.UserId, request.Title, request.Body);

            await _questionRepository.AddAsync(question, ct);

            if (request.Tags?.Count > 0)
            {
                var tagNames = request.Tags
                    .Select(t => t?.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var tagName in tagNames!)
                {
                    var tag = await _tagRepository.GetOrCreateAsync(tagName!, ct);
                    var questionTag = new QuestionTag
                    {
                        Question = question
                    };

                    if (tag.TagId > 0)
                    {
                        questionTag.TagId = tag.TagId;
                    }
                    else
                    {
                        questionTag.Tag = tag;
                    }
                    _questionRepository.AddQuestionTag(questionTag);
                }
            }

            await _userRepository.UpdateReputationAsync(
                request.UserId,
                Shared.Constants.ReputationPoints.AskQuestion,
                ct);

            await _unitOfWork.SaveChangesAsync(ct);

            _cacheService?.RemoveByPrefix("questions:page:");
            _cacheService?.RemoveByPrefix("search:");

            return new QuestionDto
            {
                QuestionId = question.QuestionId,
                Title = question.Title,
                Body = question.Body,
                CreatedDate = DateTime.SpecifyKind(question.CreatedDate, DateTimeKind.Utc),
                Status = question.Status,
                ViewCount = question.ViewCount,
                Score = 0
            };
        }, cancellationToken);
    }
}
