using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Infrastructure.RabbitMQ;

/// <summary>
/// Polls the SQL OutboxMessages table and publishes events to RabbitMQ.
/// Guarantees at-least-once delivery when outbox rows are written in the same SQL transaction.
/// </summary>
public class SqlOutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly ILogger<SqlOutboxProcessor> _logger;
    private IChannel? _channel;
    private bool _channelReady;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7);
    private const int BatchSize = 50;

    public SqlOutboxProcessor(
        IServiceProvider serviceProvider,
        IConnection connection,
        ILogger<SqlOutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _connection = connection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SqlOutboxProcessor starting...");

        var lastCleanup = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureChannelAsync(stoppingToken);
                await ProcessBatchAsync(stoppingToken);

                if (DateTime.UtcNow - lastCleanup > CleanupInterval)
                {
                    await CleanupAsync(stoppingToken);
                    lastCleanup = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SQL outbox processing cycle");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task EnsureChannelAsync(CancellationToken ct)
    {
        if (_channelReady) return;

        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
        await _channel.ExchangeDeclareAsync(
            exchange: SocialEventPublisher.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);
        _channelReady = true;
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var pending = await outboxRepo.GetPendingAsync(BatchSize, ct);
        if (pending.Count == 0) return;

        foreach (var msg in pending)
        {
            try
            {
                var routingKey = msg.EventType;
                var body = Encoding.UTF8.GetBytes(msg.PayloadJson);

                string exchange;
                if (routingKey.StartsWith("like.") || routingKey.StartsWith("vote."))
                    exchange = SocialEventPublisher.ExchangeName;
                else if (routingKey.StartsWith("message.") || routingKey.StartsWith("reaction."))
                    exchange = RabbitMqChatMessageBroker.ExchangeName;
                else
                    exchange = SocialEventPublisher.ExchangeName;

                var props = new BasicProperties
                {
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent,
                    Timestamp = new AmqpTimestamp(new DateTimeOffset(msg.CreatedAt).ToUnixTimeSeconds()),
                    MessageId = msg.Id.ToString()
                };

                await _channel!.BasicPublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: body,
                    cancellationToken: ct);

                await outboxRepo.MarkProcessedAsync(msg.Id, ct);
                _logger.LogDebug("SQL outbox event {Id} ({EventType}) published", msg.Id, msg.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish SQL outbox event {Id}, retry {Retry}",
                    msg.Id, msg.RetryCount);
                await outboxRepo.IncrementRetryAsync(msg.Id, ct);
            }
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            await outboxRepo.CleanupProcessedAsync(RetentionPeriod, ct);
            _logger.LogInformation("SQL outbox cleanup completed (retention: {Days} days)", RetentionPeriod.TotalDays);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL outbox cleanup failed");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }
        await base.StopAsync(cancellationToken);
    }
}
