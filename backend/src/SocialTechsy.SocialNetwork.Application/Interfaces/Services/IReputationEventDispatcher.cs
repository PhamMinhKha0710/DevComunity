namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IReputationEventDispatcher
{
    Task NotifyReputationChangedAsync(int userId, int newScore, int delta, string reason, CancellationToken cancellationToken = default);
}
