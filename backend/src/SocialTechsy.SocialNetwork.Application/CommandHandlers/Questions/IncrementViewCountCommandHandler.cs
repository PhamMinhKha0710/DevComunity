using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for IncrementViewCountCommand - records a question view (fire-and-forget safe)
/// </summary>
public class IncrementViewCountCommandHandler : IRequestHandler<IncrementViewCountCommand>
{
    private readonly IQuestionRepository _questionRepository;

    public IncrementViewCountCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task Handle(IncrementViewCountCommand request, CancellationToken cancellationToken)
    {
        await _questionRepository.IncrementViewCountAsync(request.QuestionId, cancellationToken);
    }
}
