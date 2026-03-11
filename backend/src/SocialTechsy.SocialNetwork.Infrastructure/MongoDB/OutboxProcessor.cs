using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.MongoDB.Models;

namespace SocialTechsy.SocialNetwork.Infrastructure.MongoDB;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox processing cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessPendingEventsAsync(CancellationToken ct)
    {
        var mongoDb = _serviceProvider.GetService<IMongoDatabase>();
        var broker = _serviceProvider.GetService<IChatMessageBroker>();
        if (mongoDb == null || broker == null) return;

        var outbox = mongoDb.GetCollection<OutboxDocument>("outbox");
        var filter = Builders<OutboxDocument>.Filter.And(
            Builders<OutboxDocument>.Filter.Eq(e => e.Processed, false),
            Builders<OutboxDocument>.Filter.Lt(e => e.RetryCount, 5)
        );

        var pending = await outbox.Find(filter)
            .Sort(Builders<OutboxDocument>.Sort.Ascending(e => e.CreatedAt))
            .Limit(50)
            .ToListAsync(ct);

        foreach (var evt in pending)
        {
            try
            {
                await broker.PublishAsync(new ChatEvent
                {
                    Type = evt.EventType,
                    PayloadJson = evt.PayloadJson,
                    Timestamp = evt.CreatedAt
                });

                var update = Builders<OutboxDocument>.Update
                    .Set(e => e.Processed, true)
                    .Set(e => e.ProcessedAt, DateTime.UtcNow);
                await outbox.UpdateOneAsync(
                    Builders<OutboxDocument>.Filter.Eq(e => e.Id, evt.Id), update, cancellationToken: ct);

                _logger.LogDebug("Outbox event {Id} published successfully", evt.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish outbox event {Id}, retry {RetryCount}", evt.Id, evt.RetryCount);
                var update = Builders<OutboxDocument>.Update.Inc(e => e.RetryCount, 1);
                await outbox.UpdateOneAsync(
                    Builders<OutboxDocument>.Filter.Eq(e => e.Id, evt.Id), update, cancellationToken: ct);
            }
        }
    }
}
