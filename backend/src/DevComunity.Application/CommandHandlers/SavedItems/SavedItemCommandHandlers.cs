using DevComunity.Application.Commands.SavedItems;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;

namespace DevComunity.Application.CommandHandlers.SavedItems;

/// <summary>
/// Handler for saving a question
/// </summary>
public class SaveQuestionCommandHandler
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IQuestionRepository _questionRepository;

    public SaveQuestionCommandHandler(
        ISavedItemRepository savedItemRepository,
        IQuestionRepository questionRepository)
    {
        _savedItemRepository = savedItemRepository;
        _questionRepository = questionRepository;
    }

    public async Task<bool> HandleAsync(SaveQuestionCommand command, CancellationToken cancellationToken)
    {
        // Check if question exists
        if (!await _questionRepository.ExistsAsync(command.QuestionId, cancellationToken))
            return false;

        // Check if already saved
        if (await _savedItemRepository.IsSavedAsync(command.UserId, command.QuestionId, null, cancellationToken))
            return true; // Already saved

        var savedItem = new SavedItem
        {
            UserId = command.UserId,
            QuestionId = command.QuestionId,
            CreatedDate = DateTime.UtcNow
        };

        await _savedItemRepository.AddAsync(savedItem, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for unsaving a question
/// </summary>
public class UnsaveQuestionCommandHandler
{
    private readonly ISavedItemRepository _savedItemRepository;

    public UnsaveQuestionCommandHandler(ISavedItemRepository savedItemRepository)
    {
        _savedItemRepository = savedItemRepository;
    }

    public async Task HandleAsync(UnsaveQuestionCommand command, CancellationToken cancellationToken)
    {
        await _savedItemRepository.DeleteByQuestionAsync(command.UserId, command.QuestionId, cancellationToken);
    }
}

/// <summary>
/// Handler for saving an answer
/// </summary>
public class SaveAnswerCommandHandler
{
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IAnswerRepository _answerRepository;

    public SaveAnswerCommandHandler(
        ISavedItemRepository savedItemRepository,
        IAnswerRepository answerRepository)
    {
        _savedItemRepository = savedItemRepository;
        _answerRepository = answerRepository;
    }

    public async Task<bool> HandleAsync(SaveAnswerCommand command, CancellationToken cancellationToken)
    {
        // Check if answer exists
        var answer = await _answerRepository.GetByIdAsync(command.AnswerId, cancellationToken);
        if (answer == null)
            return false;

        // Check if already saved
        if (await _savedItemRepository.IsSavedAsync(command.UserId, null, command.AnswerId, cancellationToken))
            return true; // Already saved

        var savedItem = new SavedItem
        {
            UserId = command.UserId,
            AnswerId = command.AnswerId,
            CreatedDate = DateTime.UtcNow
        };

        await _savedItemRepository.AddAsync(savedItem, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for unsaving an answer
/// </summary>
public class UnsaveAnswerCommandHandler
{
    private readonly ISavedItemRepository _savedItemRepository;

    public UnsaveAnswerCommandHandler(ISavedItemRepository savedItemRepository)
    {
        _savedItemRepository = savedItemRepository;
    }

    public async Task HandleAsync(UnsaveAnswerCommand command, CancellationToken cancellationToken)
    {
        await _savedItemRepository.DeleteByAnswerAsync(command.UserId, command.AnswerId, cancellationToken);
    }
}
