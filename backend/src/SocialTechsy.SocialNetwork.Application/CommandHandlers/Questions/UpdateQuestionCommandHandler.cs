using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Common.Exceptions;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for UpdateQuestionCommand
/// </summary>
public class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand, Unit>
{
    private readonly IQuestionRepository _questionRepository;

    public UpdateQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
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
        return Unit.Value;
    }
}
