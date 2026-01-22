using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for User entity
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly DevComunityDbContext _context;

    public UserRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Badges)
            .FirstOrDefaultAsync(u => u.UserId == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<(User User, int QuestionCount, int AnswerCount)?> GetUserWithStatsAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Badges)
            .FirstOrDefaultAsync(u => u.UserId == id, cancellationToken);

        if (user == null)
            return null;

        var questionCount = await _context.Questions.CountAsync(q => q.UserId == id, cancellationToken);
        var answerCount = await _context.Answers.CountAsync(a => a.UserId == id, cancellationToken);

        return (user, questionCount, answerCount);
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPaginatedAsync(
        int page, 
        int pageSize, 
        string? search, 
        string sortBy, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsQueryable();

        // Filter by search
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => 
                u.Username.Contains(search) || 
                (u.DisplayName != null && u.DisplayName.Contains(search)));
        }

        // Sort
        query = sortBy switch
        {
            "newest" => query.OrderByDescending(u => u.CreatedDate),
            _ => query.OrderByDescending(u => u.ReputationPoints) // reputation
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task UpdateReputationAsync(int userId, int change, CancellationToken cancellationToken = default)
    {
        // Atomic update ensuring reputation doesn't go below 1
        await _context.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                u => u.ReputationPoints, 
                u => u.ReputationPoints + change < 1 ? 1 : u.ReputationPoints + change
            ), cancellationToken);
    }
}

