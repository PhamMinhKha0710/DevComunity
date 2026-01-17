using DevComunity.Application.Commands.Questions;
using DevComunity.Application.Interfaces.Repositories;

namespace DevComunity.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for UpdateQuestionCommand
/// </summary>
public class UpdateQuestionCommandHandler
{
    private readonly IQuestionRepository _questionRepository;

    public UpdateQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<bool> HandleAsync(UpdateQuestionCommand command, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        
        if (question == null || question.UserId != command.UserId)
            return false;

        question.Title = command.Title;
        question.Body = command.Body;
        question.UpdatedDate = DateTime.UtcNow;

        await _questionRepository.UpdateAsync(question, cancellationToken);
        return true;
    }
}
