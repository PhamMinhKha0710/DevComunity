using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.RabbitMQ;

public class SocialEventPublisher : ISocialEventPublisher, IAsyncDisposable
{
    public const string ExchangeName = "social.events";

    private readonly IConnection _connection;
    private readonly ILogger<SocialEventPublisher> _logger;
    private IChannel? _channel;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public SocialEventPublisher(IConnection connection, ILogger<SocialEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            _channel = await _connection.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishLikeEventAsync(LikeEvent evt)
    {
        try
        {
            await EnsureInitializedAsync();

            var routingKey = $"like.{evt.TargetType}";
            var json = JsonSerializer.Serialize(evt);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                MessageId = evt.EventId
            };

            await _channel!.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);

            _logger.LogDebug("Published like event: {RoutingKey} for {TargetType}:{TargetId}", routingKey, evt.TargetType, evt.TargetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish like event for {TargetType}:{TargetId}", evt.TargetType, evt.TargetId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }
    }
}
