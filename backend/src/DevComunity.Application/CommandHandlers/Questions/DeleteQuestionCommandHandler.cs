using DevComunity.Application.Commands.Questions;
using DevComunity.Application.Interfaces.Repositories;

namespace DevComunity.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for DeleteQuestionCommand
/// </summary>
public class DeleteQuestionCommandHandler
{
    private readonly IQuestionRepository _questionRepository;

    public DeleteQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<bool> HandleAsync(DeleteQuestionCommand command, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        
        if (question == null || question.UserId != command.UserId)
            return false;

        await _questionRepository.DeleteAsync(command.QuestionId, cancellationToken);
        return true;
    }
}
