using DevComunity.Application.Commands.Votes;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;

namespace DevComunity.Application.CommandHandlers.Votes;

/// <summary>
/// Handler for voting on a question
/// </summary>
public class VoteQuestionCommandHandler
{
    private readonly IVoteRepository _voteRepository;
    private readonly IQuestionRepository _questionRepository;

    public VoteQuestionCommandHandler(
        IVoteRepository voteRepository,
        IQuestionRepository questionRepository)
    {
        _voteRepository = voteRepository;
        _questionRepository = questionRepository;
    }

    public async Task<VoteResult> HandleAsync(VoteQuestionCommand command, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        if (question == null)
            return new VoteResult { Success = false, Message = "Question not found" };

        var existingVote = await _voteRepository.GetUserVoteOnQuestionAsync(
            command.UserId, command.QuestionId, cancellationToken);

        var isUpvote = command.VoteType.ToLower() == "up";

        if (existingVote != null)
        {
            // Update existing vote
            existingVote.IsUpvote = isUpvote;
            await _voteRepository.UpdateAsync(existingVote, cancellationToken);
        }
        else
        {
            // Create new vote
            var vote = new Vote
            {
                UserId = command.UserId,
                QuestionId = command.QuestionId,
                IsUpvote = isUpvote,
                CreatedDate = DateTime.UtcNow
            };
            await _voteRepository.AddAsync(vote, cancellationToken);
        }

        // Calculate new score
        var score = await _voteRepository.GetQuestionScoreAsync(command.QuestionId, cancellationToken);

        return new VoteResult
        {
            Success = true,
            Score = score,
            UserVote = command.VoteType
        };
    }
}

/// <summary>
/// Handler for voting on an answer
/// </summary>
public class VoteAnswerCommandHandler
{
    private readonly IVoteRepository _voteRepository;
    private readonly IAnswerRepository _answerRepository;

    public VoteAnswerCommandHandler(
        IVoteRepository voteRepository,
        IAnswerRepository answerRepository)
    {
        _voteRepository = voteRepository;
        _answerRepository = answerRepository;
    }

    public async Task<VoteResult> HandleAsync(VoteAnswerCommand command, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(command.AnswerId, cancellationToken);
        if (answer == null)
            return new VoteResult { Success = false, Message = "Answer not found" };

        var existingVote = await _voteRepository.GetUserVoteOnAnswerAsync(
            command.UserId, command.AnswerId, cancellationToken);

        var isUpvote = command.VoteType.ToLower() == "up";

        if (existingVote != null)
        {
            existingVote.IsUpvote = isUpvote;
            await _voteRepository.UpdateAsync(existingVote, cancellationToken);
        }
        else
        {
            var vote = new Vote
            {
                UserId = command.UserId,
                AnswerId = command.AnswerId,
                IsUpvote = isUpvote,
                CreatedDate = DateTime.UtcNow
            };
            await _voteRepository.AddAsync(vote, cancellationToken);
        }

        var score = await _voteRepository.GetAnswerScoreAsync(command.AnswerId, cancellationToken);

        return new VoteResult
        {
            Success = true,
            Score = score,
            UserVote = command.VoteType
        };
    }
}

/// <summary>
/// Handler for removing a vote
/// </summary>
public class RemoveVoteCommandHandler
{
    private readonly IVoteRepository _voteRepository;

    public RemoveVoteCommandHandler(IVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public async Task<VoteResult> HandleAsync(RemoveVoteCommand command, CancellationToken cancellationToken)
    {
        if (command.QuestionId.HasValue)
        {
            var vote = await _voteRepository.GetUserVoteOnQuestionAsync(
                command.UserId, command.QuestionId.Value, cancellationToken);
            if (vote != null)
            {
                await _voteRepository.DeleteAsync(vote.VoteId, cancellationToken);
            }
            var score = await _voteRepository.GetQuestionScoreAsync(command.QuestionId.Value, cancellationToken);
            return new VoteResult { Success = true, Score = score };
        }
        else if (command.AnswerId.HasValue)
        {
            var vote = await _voteRepository.GetUserVoteOnAnswerAsync(
                command.UserId, command.AnswerId.Value, cancellationToken);
            if (vote != null)
            {
                await _voteRepository.DeleteAsync(vote.VoteId, cancellationToken);
            }
            var score = await _voteRepository.GetAnswerScoreAsync(command.AnswerId.Value, cancellationToken);
            return new VoteResult { Success = true, Score = score };
        }

        return new VoteResult { Success = false, Message = "Invalid vote target" };
    }
}

/// <summary>
/// Result of a vote operation
/// </summary>
public class VoteResult
{
    public bool Success { get; set; }
    public int Score { get; set; }
    public string? UserVote { get; set; }
    public string? Message { get; set; }
}

