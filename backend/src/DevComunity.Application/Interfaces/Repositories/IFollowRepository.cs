using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for UserFollow entity
/// </summary>
public interface IFollowRepository
{
    Task<bool> IsFollowingAsync(int followerId, int followingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserFollow>> GetFollowersAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserFollow>> GetFollowingAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserFollow> FollowAsync(UserFollow follow, CancellationToken cancellationToken = default);
    Task UnfollowAsync(int followerId, int followingId, CancellationToken cancellationToken = default);
    Task<int> GetFollowersCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetFollowingCountAsync(int userId, CancellationToken cancellationToken = default);
}
