using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Group and GroupMember entities
/// </summary>
public interface IGroupRepository
{
    // Group CRUD
    Task<Group?> GetByIdAsync(int groupId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Group> Items, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<IEnumerable<Group>> GetUserGroupsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default);
    Task UpdateAsync(Group group, CancellationToken cancellationToken = default);
    Task DeleteAsync(int groupId, CancellationToken cancellationToken = default);

    // Member operations
    Task<GroupMember?> GetMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GroupMember>> GetMembersAsync(int groupId, CancellationToken cancellationToken = default);
    Task<GroupMember> AddMemberAsync(GroupMember member, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default);
    Task UpdateMemberRoleAsync(int groupId, int userId, GroupRole role, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default);
    Task<int> GetMemberCountAsync(int groupId, CancellationToken cancellationToken = default);

    // Batch operations to avoid N+1
    Task<Dictionary<int, int>> GetMemberCountsBatchAsync(IEnumerable<int> groupIds, CancellationToken cancellationToken = default);
    Task<Dictionary<int, GroupMember?>> GetUserMembershipsBatchAsync(IEnumerable<int> groupIds, int userId, CancellationToken cancellationToken = default);
}
