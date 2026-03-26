using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Vote entity
/// </summary>
public class VoteRepository : IVoteRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public VoteRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    public async Task<Vote?> GetUserVoteAsync(int userId, int? questionId, int? answerId, CancellationToken cancellationToken = default)
    {
        return await _context.Votes
            .FirstOrDefaultAsync(v => 
                v.UserId == userId && 
                v.QuestionId == questionId && 
                v.AnswerId == answerId, 
                cancellationToken);
    }

    public async Task<Vote?> GetUserVoteOnQuestionAsync(int userId, int questionId, CancellationToken cancellationToken = default)
    {
        return await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.QuestionId == questionId, cancellationToken);
    }

    public async Task<Vote?> GetUserVoteOnAnswerAsync(int userId, int answerId, CancellationToken cancellationToken = default)
    {
        return await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.AnswerId == answerId, cancellationToken);
    }

    public async Task<Vote> AddAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        await _context.Votes.AddAsync(vote, cancellationToken);
        return vote;
    }

    public async Task UpdateAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        _context.Votes.Update(vote);
    }

    public async Task DeleteAsync(int voteId, CancellationToken cancellationToken = default)
    {
        var vote = await _context.Votes.FindAsync(new object[] { voteId }, cancellationToken);
        if (vote != null)
        {
            _context.Votes.Remove(vote);
        }
    }

    public async Task<int> GetScoreAsync(int? questionId, int? answerId, CancellationToken cancellationToken = default)
    {
        return await _context.Votes
            .Where(v => v.QuestionId == questionId && v.AnswerId == answerId)
            .SumAsync(v => v.IsUpvote ? 1 : -1, cancellationToken);
    }

    public async Task<int> GetQuestionScoreAsync(int questionId, CancellationToken cancellationToken = default)
    {
        return await _context.Votes
            .Where(v => v.QuestionId == questionId)
            .SumAsync(v => v.IsUpvote ? 1 : -1, cancellationToken);
    }

    public async Task<int> GetAnswerScoreAsync(int answerId, CancellationToken cancellationToken = default)
    {
        return await _context.Votes
            .Where(v => v.AnswerId == answerId)
            .SumAsync(v => v.IsUpvote ? 1 : -1, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetAnswerScoresForAnswerIdsAsync(
        int[] answerIds,
        CancellationToken cancellationToken = default)
    {
        if (answerIds.Length == 0)
            return new Dictionary<int, int>();

        var distinct = answerIds.Distinct().ToArray();
        var grouped = await _context.Votes
            .AsNoTracking()
            .Where(v => v.AnswerId != null && distinct.Contains(v.AnswerId.Value))
            .GroupBy(v => v.AnswerId!.Value)
            .Select(g => new { AnswerId = g.Key, Score = g.Sum(v => v.IsUpvote ? 1 : -1) })
            .ToListAsync(cancellationToken);

        var map = grouped.ToDictionary(x => x.AnswerId, x => x.Score);
        return distinct.ToDictionary(id => id, id => map.GetValueOrDefault(id, 0));
    }

    public async Task<IReadOnlyDictionary<int, Vote>> GetUserVotesForAnswerIdsAsync(
        int userId,
        int[] answerIds,
        CancellationToken cancellationToken = default)
    {
        if (answerIds.Length == 0)
            return new Dictionary<int, Vote>();

        var distinct = answerIds.Distinct().ToArray();
        return await _context.Votes
            .AsNoTracking()
            .Where(v => v.UserId == userId && v.AnswerId != null && distinct.Contains(v.AnswerId.Value))
            .ToDictionaryAsync(v => v.AnswerId!.Value, v => v, cancellationToken);
    }
}


