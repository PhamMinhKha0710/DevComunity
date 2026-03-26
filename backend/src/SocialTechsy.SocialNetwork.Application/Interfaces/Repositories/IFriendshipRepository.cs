using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Friendship entity
/// </summary>
public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(int friendshipId, CancellationToken cancellationToken = default);
    Task<Friendship?> GetFriendshipAsync(int requesterId, int addresseeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Friendship>> GetFriendsAsync(int userId, CancellationToken cancellationToken = default);

    Task<(IEnumerable<Friendship> Items, int TotalCount)> GetFriendsPagedAsync(
        int userId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Friendship>> GetPendingRequestsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Friendship>> GetSentRequestsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Friendship> AddAsync(Friendship friendship, CancellationToken cancellationToken = default);
    Task UpdateAsync(Friendship friendship, CancellationToken cancellationToken = default);
    Task DeleteAsync(int friendshipId, CancellationToken cancellationToken = default);
    Task<bool> AreFriendsAsync(int userId1, int userId2, CancellationToken cancellationToken = default);
    Task<int> GetFriendsCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FriendDto>> GetSuggestedFriendsAsync(int userId, int limit = 5, CancellationToken cancellationToken = default);
    Task<NetworkGrowthDto> GetNetworkGrowthAsync(int userId, int days = 28, CancellationToken cancellationToken = default);
}
