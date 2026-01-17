using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Answer entity
/// </summary>
public class AnswerRepository : IAnswerRepository
{
    private readonly DevComunityDbContext _context;

    public AnswerRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<Answer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Answers
            .Include(a => a.User)
            .Include(a => a.Comments)
                .ThenInclude(c => c.User)
            .Include(a => a.ChildAnswers)
            .FirstOrDefaultAsync(a => a.AnswerId == id, cancellationToken);
    }

    public async Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId, CancellationToken cancellationToken = default)
    {
        return await _context.Answers
            .Include(a => a.User)
            .Include(a => a.Comments)
                .ThenInclude(c => c.User)
            .Include(a => a.ChildAnswers)
            .Where(a => a.QuestionId == questionId && a.ParentAnswerId == null)
            .OrderByDescending(a => a.IsAccepted)
            .ThenByDescending(a => a.Score)
            .ToListAsync(cancellationToken);
    }

    public async Task<Answer> AddAsync(Answer answer, CancellationToken cancellationToken = default)
    {
        await _context.Answers.AddAsync(answer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return answer;
    }

    public async Task UpdateAsync(Answer answer, CancellationToken cancellationToken = default)
    {
        _context.Answers.Update(answer);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var answer = await _context.Answers.FindAsync(new object[] { id }, cancellationToken);
        if (answer != null)
        {
            _context.Answers.Remove(answer);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> AcceptAnswerAsync(int answerId, int questionId, CancellationToken cancellationToken = default)
    {
        // Unaccept any previously accepted answer
        var previouslyAccepted = await _context.Answers
            .Where(a => a.QuestionId == questionId && a.IsAccepted)
            .ToListAsync(cancellationToken);

        foreach (var answer in previouslyAccepted)
        {
            answer.IsAccepted = false;
        }

        // Accept the new answer
        var newAccepted = await _context.Answers.FindAsync(new object[] { answerId }, cancellationToken);
        if (newAccepted == null || newAccepted.QuestionId != questionId)
            return false;

        newAccepted.IsAccepted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
