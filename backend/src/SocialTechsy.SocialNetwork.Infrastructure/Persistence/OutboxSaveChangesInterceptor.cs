using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence;

/// <summary>
/// EF Core interceptor that scans tracked entities for pending domain events
/// and writes them to the OutboxMessages table in the same SaveChanges transaction.
/// Entities must implement IHasDomainEvents to participate.
/// </summary>
public class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return ValueTask.FromResult(result);

        var context = eventData.Context;
        var domainEntities = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        if (domainEntities.Count == 0) return ValueTask.FromResult(result);

        var outboxMessages = domainEntities
            .SelectMany(entry =>
            {
                var events = entry.Entity.DomainEvents.ToList();
                entry.Entity.ClearDomainEvents();
                return events;
            })
            .Select(domainEvent => new OutboxMessage
            {
                EventType = $"domain.{domainEvent.GetType().Name}",
                PayloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                CreatedAt = DateTime.UtcNow
            });

        context.Set<OutboxMessage>().AddRange(outboxMessages);

        return ValueTask.FromResult(result);
    }
}
