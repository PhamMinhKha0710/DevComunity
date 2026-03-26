using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Comment entity
/// </summary>
public class CommentRepository : ICommentRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public CommentRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    public async Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CommentId == id, cancellationToken);
    }

    public async Task<IEnumerable<Comment>> GetByQuestionIdAsync(int questionId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.QuestionId == questionId)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Comment>> GetByAnswerIdAsync(int answerId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.AnswerId == answerId)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Comment>> GetByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Where(c => c.PostId == postId)
            .CountAsync(cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetCountsByPostIdsAsync(IEnumerable<int> postIds, CancellationToken cancellationToken = default)
    {
        var idList = postIds.ToList();
        if (!idList.Any()) return new Dictionary<int, int>();

        var counts = await _context.Comments
            .Where(c => c.PostId.HasValue && idList.Contains(c.PostId.Value))
            .GroupBy(c => c.PostId!.Value)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = idList.ToDictionary(id => id, _ => 0);
        foreach (var c in counts)
        {
            result[c.PostId] = c.Count;
        }
        return result;
    }

    public async Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _context.Comments.AddAsync(comment, cancellationToken);
        return comment;
    }

    public async Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        _context.Comments.Update(comment);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var comment = await _context.Comments.FindAsync(new object[] { id }, cancellationToken);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
        }
    }
}
