namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IViewService
{
    Task<long> IncrementViewAsync(int questionId, string? userId, string? ip);
    Task<long> GetViewCountAsync(int questionId);
    Task<Dictionary<int, long>> GetViewCountsBatchAsync(int[] questionIds);
}
