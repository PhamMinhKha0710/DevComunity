using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Vote entity
/// </summary>
public class VoteRepository : IVoteRepository
{
    private readonly DevComunityDbContext _context;

    public VoteRepository(DevComunityDbContext context)
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
        await _context.SaveChangesAsync(cancellationToken);
        return vote;
    }

    public async Task UpdateAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        _context.Votes.Update(vote);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int voteId, CancellationToken cancellationToken = default)
    {
        var vote = await _context.Votes.FindAsync(new object[] { voteId }, cancellationToken);
        if (vote != null)
        {
            _context.Votes.Remove(vote);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetScoreAsync(int? questionId, int? answerId, CancellationToken cancellationToken = default)
    {
        var votes = await _context.Votes
            .Where(v => v.QuestionId == questionId && v.AnswerId == answerId)
            .ToListAsync(cancellationToken);
        return votes.Sum(v => v.IsUpvote ? 1 : -1);
    }

    public async Task<int> GetQuestionScoreAsync(int questionId, CancellationToken cancellationToken = default)
    {
        var votes = await _context.Votes
            .Where(v => v.QuestionId == questionId)
            .ToListAsync(cancellationToken);
        return votes.Sum(v => v.IsUpvote ? 1 : -1);
    }

    public async Task<int> GetAnswerScoreAsync(int answerId, CancellationToken cancellationToken = default)
    {
        var votes = await _context.Votes
            .Where(v => v.AnswerId == answerId)
            .ToListAsync(cancellationToken);
        return votes.Sum(v => v.IsUpvote ? 1 : -1);
    }
}


