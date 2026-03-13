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
    private bool _indexesEnsured;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor starting...");

        await EnsureIndexesAsync();

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

    private async Task EnsureIndexesAsync()
    {
        if (_indexesEnsured) return;

        try
        {
            var mongoDb = _serviceProvider.GetService<IMongoDatabase>();
            if (mongoDb == null) return;

            var outbox = mongoDb.GetCollection<OutboxDocument>("outbox");

            await outbox.Indexes.CreateOneAsync(new CreateIndexModel<OutboxDocument>(
                Builders<OutboxDocument>.IndexKeys
                    .Ascending(e => e.Processed)
                    .Ascending(e => e.CreatedAt),
                new CreateIndexOptions { Background = true, Name = "IX_Outbox_Pending" }));

            // TTL index: auto-delete documents 7 days after ProcessedAt is set.
            // Documents with ProcessedAt=null (unprocessed) are ignored by MongoDB TTL.
            await outbox.Indexes.CreateOneAsync(new CreateIndexModel<OutboxDocument>(
                Builders<OutboxDocument>.IndexKeys.Ascending(e => e.ProcessedAt),
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.FromDays(7),
                    Background = true,
                    Name = "TTL_Outbox_Processed_7d"
                }));

            _indexesEnsured = true;
            _logger.LogInformation("Outbox indexes ensured (including 7-day TTL for processed events)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create outbox indexes");
        }
    }
}
