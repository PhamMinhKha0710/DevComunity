namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IActivityLogService
{
    Task LogLikeAsync(string targetType, int targetId, int userId);
    Task LogUnlikeAsync(string targetType, int targetId, int userId);
    Task LogViewAsync(int questionId, int? userId, string? ip, string? userAgent);
}
