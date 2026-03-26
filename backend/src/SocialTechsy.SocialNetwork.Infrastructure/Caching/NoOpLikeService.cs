using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public sealed class NoOpLikeService : ILikeService
{
    public Task<long> LikeAsync(string targetType, int targetId, int userId) => Task.FromResult(0L);
    public Task<long> UnlikeAsync(string targetType, int targetId, int userId) => Task.FromResult(0L);
    public Task<bool> IsLikedAsync(string targetType, int targetId, int userId) => Task.FromResult(false);
    public Task<long> GetLikeCountAsync(string targetType, int targetId) => Task.FromResult(0L);
    public Task<Dictionary<int, long>> GetLikeCountsBatchAsync(string targetType, int[] targetIds)
    {
        var result = new Dictionary<int, long>(targetIds.Length);
        foreach (var id in targetIds)
            result[id] = 0;
        return Task.FromResult(result);
    }
    public Task SyncFromSqlAsync(string targetType, int targetId, int sqlScore) => Task.CompletedTask;
}
