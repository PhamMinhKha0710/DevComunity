using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Friendship entity
/// </summary>
public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(int friendshipId, CancellationToken cancellationToken = default);
    Task<Friendship?> GetFriendshipAsync(int requesterId, int addresseeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Friendship>> GetFriendsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Friendship>> GetPendingRequestsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Friendship>> GetSentRequestsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Friendship> AddAsync(Friendship friendship, CancellationToken cancellationToken = default);
    Task UpdateAsync(Friendship friendship, CancellationToken cancellationToken = default);
    Task DeleteAsync(int friendshipId, CancellationToken cancellationToken = default);
    Task<bool> AreFriendsAsync(int userId1, int userId2, CancellationToken cancellationToken = default);
    Task<int> GetFriendsCountAsync(int userId, CancellationToken cancellationToken = default);
}
