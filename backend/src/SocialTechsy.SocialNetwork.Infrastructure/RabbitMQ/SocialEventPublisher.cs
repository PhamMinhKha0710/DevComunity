using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using RabbitMQ.Client;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.Resilience;

namespace SocialTechsy.SocialNetwork.Infrastructure.RabbitMQ;

public class SocialEventPublisher : ISocialEventPublisher, IAsyncDisposable
{
    public const string ExchangeName = "social.events";

    private readonly IConnection _connection;
    private readonly ILogger<SocialEventPublisher> _logger;
    private readonly ResiliencePipeline? _resiliencePipeline;
    private IChannel? _channel;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public SocialEventPublisher(
        IConnection connection,
        ILogger<SocialEventPublisher> logger,
        ResiliencePipelineProvider<string>? pipelineProvider = null)
    {
        _connection = connection;
        _logger = logger;
        pipelineProvider?.TryGetPipeline(ResiliencePolicies.RabbitMq, out _resiliencePipeline);
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
            var publishAction = async (CancellationToken ct) =>
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
                    body: body,
                    cancellationToken: ct);

                _logger.LogDebug("Published like event: {RoutingKey} for {TargetType}:{TargetId}", routingKey, evt.TargetType, evt.TargetId);
            };

            if (_resiliencePipeline != null)
                await _resiliencePipeline.ExecuteAsync(async ct => { await publishAction(ct); });
            else
                await publishAction(CancellationToken.None);
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
