using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.MessageBroker.RabbitMQ;

public class RabbitMqChatMessageBroker : IChatMessageBroker, IDisposable
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqChatMessageBroker> _logger;
    private IChannel? _channel;
    private const string ExchangeName = "chat.events";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RabbitMqChatMessageBroker(IConnection connection, ILogger<RabbitMqChatMessageBroker> logger)
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

    public async Task PublishAsync(ChatEvent chatEvent)
    {
        try
        {
            await EnsureChannelAsync();
            var routingKey = chatEvent.Type;
            var body = Encoding.UTF8.GetBytes(chatEvent.PayloadJson);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = chatEvent.EventId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel!.BasicPublishAsync(ExchangeName, routingKey, mandatory: false, basicProperties: properties, body: body);
            _logger.LogDebug("Published chat event {EventId} with routing key {RoutingKey}", chatEvent.EventId, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chat event {EventId}", chatEvent.EventId);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
