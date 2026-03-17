using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for User entity
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalProviderAsync(string provider, string providerId, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<(User User, int QuestionCount, int AnswerCount)?> GetUserWithStatsAsync(int id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Items, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string? search, string sortBy, CancellationToken cancellationToken = default);
    Task UpdateReputationAsync(int userId, int change, CancellationToken cancellationToken = default);
}

