using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.RabbitMQ;

public class RabbitMqChatMessageBroker : IChatMessageBroker, IAsyncDisposable
{
    public const string ExchangeName = "chat.events";
    public const string QueueName = "chat.events.process";

    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqChatMessageBroker> _logger;
    private IChannel? _channel;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RabbitMqChatMessageBroker(IConnection connection, ILogger<RabbitMqChatMessageBroker> logger)
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

    public async Task PublishAsync(ChatEvent chatEvent)
    {
        try
        {
            await EnsureInitializedAsync();

            var json = JsonSerializer.Serialize(chatEvent);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel!.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: chatEvent.Type,
                mandatory: false,
                basicProperties: props,
                body: body);

            _logger.LogDebug("Published chat event {Type}", chatEvent.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chat event {Type}", chatEvent.Type);
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
