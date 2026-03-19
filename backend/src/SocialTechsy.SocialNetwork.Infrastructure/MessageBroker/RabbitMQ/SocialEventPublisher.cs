using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.MessageBroker.RabbitMQ;

public class SocialEventPublisher : ISocialEventPublisher, IDisposable
{
    public const string ExchangeName = "social.events";
    private readonly IConnection _connection;
    private readonly ILogger<SocialEventPublisher> _logger;
    private IChannel? _channel;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SocialEventPublisher(IConnection connection, ILogger<SocialEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    private async Task EnsureChannelAsync()
    {
        if (_channel != null) return;
        _channel = await _connection.CreateChannelAsync(cancellationToken: CancellationToken.None);
        await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
    }

    public async Task PublishLikeEventAsync(LikeEvent evt)
    {
        try
        {
            await EnsureChannelAsync();
            var routingKey = "like.new";
            var payload = JsonSerializer.Serialize(evt, JsonOptions);
            var body = Encoding.UTF8.GetBytes(payload);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = evt.EventId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel!.BasicPublishAsync(ExchangeName, routingKey, mandatory: false, basicProperties: properties, body: body);
            _logger.LogDebug("Published like event {EventId}", evt.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish like event {EventId}", evt.EventId);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
