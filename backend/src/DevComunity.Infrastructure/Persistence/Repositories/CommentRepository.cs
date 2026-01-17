using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Comment entity
/// </summary>
public class CommentRepository : ICommentRepository
{
    private readonly DevComunityDbContext _context;

    public CommentRepository(DevComunityDbContext context)
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

    public async Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _context.Comments.AddAsync(comment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        _context.Comments.Update(comment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var comment = await _context.Comments.FindAsync(new object[] { id }, cancellationToken);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
