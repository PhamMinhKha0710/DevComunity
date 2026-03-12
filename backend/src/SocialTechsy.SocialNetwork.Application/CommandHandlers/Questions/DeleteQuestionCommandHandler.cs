using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Common.Exceptions;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for DeleteQuestionCommand
/// </summary>
public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand, Unit>
{
    private readonly IQuestionRepository _questionRepository;

    public DeleteQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<Unit> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);

        if (question == null)
            throw new EntityNotFoundException("Question", request.QuestionId);

        if (question.UserId != request.UserId)
            throw new UnauthorizedCommandException($"User {request.UserId} is not authorized to delete Question {request.QuestionId}.");

        await _questionRepository.DeleteAsync(request.QuestionId, cancellationToken);
        return Unit.Value;
    }
}
