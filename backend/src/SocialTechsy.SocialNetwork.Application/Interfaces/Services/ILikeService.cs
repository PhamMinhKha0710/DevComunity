namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface ILikeService
{
    Task<long> LikeAsync(string targetType, int targetId, int userId);
    Task<long> UnlikeAsync(string targetType, int targetId, int userId);
    Task<bool> IsLikedAsync(string targetType, int targetId, int userId);
    Task<long> GetLikeCountAsync(string targetType, int targetId);
    Task<Dictionary<int, long>> GetLikeCountsBatchAsync(string targetType, int[] targetIds);
}
