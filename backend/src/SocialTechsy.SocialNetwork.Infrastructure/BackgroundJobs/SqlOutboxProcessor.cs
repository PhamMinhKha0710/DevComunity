using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Infrastructure.MessageBroker.RabbitMQ;

namespace SocialTechsy.SocialNetwork.Infrastructure.BackgroundJobs;

public class SqlOutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly ILogger<SqlOutboxProcessor> _logger;
    private IChannel? _channel;
    private const int PollIntervalMs = 5000;

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
        _logger.LogInformation("SqlOutboxProcessor starting");

        try
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await _channel.ExchangeDeclareAsync(
                exchange: SocialEventPublisher.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SqlOutboxProcessor failed to initialize");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(PollIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var messages = await outboxRepo.GetPendingAsync(50, cancellationToken);
        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                var routingKey = message.EventType;
                var body = Encoding.UTF8.GetBytes(message.PayloadJson);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = message.Id.ToString()
                };

                await _channel!.BasicPublishAsync(
                    exchange: SocialEventPublisher.ExchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cancellationToken);

                await outboxRepo.MarkProcessedAsync(message.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                await outboxRepo.IncrementRetryAsync(message.Id, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
