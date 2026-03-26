namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IMediaOwnershipChecker
{
    Task<bool> IsOwnerAsync(string mediaUrl, int userId, CancellationToken ct = default);
}
