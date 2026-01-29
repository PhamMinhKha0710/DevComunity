using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Post entity
/// </summary>
public interface IPostRepository
{
    Task<Post?> GetByIdAsync(int postId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Post> Items, int TotalCount)> GetNewsfeedAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Post> Items, int TotalCount)> GetGroupPostsAsync(int groupId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Post> Items, int TotalCount)> GetUserPostsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default);
    Task UpdateAsync(Post post, CancellationToken cancellationToken = default);
    Task DeleteAsync(int postId, CancellationToken cancellationToken = default);
}
