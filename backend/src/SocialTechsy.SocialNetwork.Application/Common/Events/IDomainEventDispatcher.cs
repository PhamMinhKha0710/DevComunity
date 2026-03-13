using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.Common.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
