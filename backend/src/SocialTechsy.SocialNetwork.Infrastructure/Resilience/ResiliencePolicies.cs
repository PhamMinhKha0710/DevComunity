using MailKit.Net.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using RabbitMQ.Client.Exceptions;
using StackExchange.Redis;

using SmtpCommandException = MailKit.Net.Smtp.SmtpCommandException;
using SmtpProtoException = MailKit.Net.Smtp.SmtpProtocolException;

namespace SocialTechsy.SocialNetwork.Infrastructure.Resilience;

public static class ResiliencePolicies
{
    public const string Redis = "redis";
    public const string Mongo = "mongo";
    public const string RabbitMq = "rabbitmq";
    public const string Smtp = "smtp";

    public static IServiceCollection AddResiliencePipelines(this IServiceCollection services)
    {
        services.AddResiliencePipeline(Redis, (builder, context) =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(100),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        ex is RedisConnectionException
                        || ex is RedisTimeoutException
                        || ex is TimeoutException),
                    OnRetry = args =>
                    {
                        var logger = context.ServiceProvider.GetService<ILoggerFactory>()?
                            .CreateLogger("Resilience.Redis");
                        logger?.LogWarning("Redis retry #{Attempt} after {Delay}ms",
                            args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(30),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        ex is RedisConnectionException
                        || ex is RedisTimeoutException),
                    OnOpened = args =>
                    {
                        var logger = context.ServiceProvider.GetService<ILoggerFactory>()?
                            .CreateLogger("Resilience.Redis");
                        logger?.LogWarning("Redis circuit breaker OPENED for {Duration}s",
                            args.BreakDuration.TotalSeconds);
                        return ValueTask.CompletedTask;
                    }
                });
        });

        services.AddResiliencePipeline(Mongo, (builder, context) =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(200),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        ex is MongoConnectionException
                        || ex is TimeoutException),
                    OnRetry = args =>
                    {
                        var logger = context.ServiceProvider.GetService<ILoggerFactory>()?
                            .CreateLogger("Resilience.Mongo");
                        logger?.LogWarning("MongoDB retry #{Attempt} after {Delay}ms",
                            args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(30),
                    ShouldHandle = new PredicateBuilder().Handle<MongoConnectionException>()
                });
        });

        services.AddResiliencePipeline(RabbitMq, (builder, context) =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(200),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        ex is BrokerUnreachableException
                        || ex is AlreadyClosedException
                        || ex is System.IO.IOException),
                    OnRetry = args =>
                    {
                        var logger = context.ServiceProvider.GetService<ILoggerFactory>()?
                            .CreateLogger("Resilience.RabbitMQ");
                        logger?.LogWarning("RabbitMQ retry #{Attempt} after {Delay}ms",
                            args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(60),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<BrokerUnreachableException>()
                        .Handle<AlreadyClosedException>()
                });
        });

        services.AddResiliencePipeline(Smtp, (builder, context) =>
        {
            builder
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(30),
                    OnTimeout = args =>
                    {
                        var logger = context.ServiceProvider.GetService<ILoggerFactory>()?
                            .CreateLogger("Resilience.Smtp");
                        logger?.LogWarning("SMTP timeout after {Timeout}s", args.Timeout.TotalSeconds);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<SmtpCommandException>()
                        .Handle<SmtpProtoException>()
                        .Handle<System.IO.IOException>()
                        .Handle<TimeoutException>(),
                    OnRetry = args =>
                    {
                        var logger = context.ServiceProvider.GetService<ILoggerFactory>()?
                            .CreateLogger("Resilience.Smtp");
                        logger?.LogWarning("SMTP retry #{Attempt} after {Delay}s. Exception: {Message}",
                            args.AttemptNumber, args.RetryDelay.TotalSeconds, args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(60),
                    MinimumThroughput = 3,
                    BreakDuration = TimeSpan.FromMinutes(5),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<SmtpCommandException>()
                        .Handle<SmtpProtoException>()
                        .Handle<System.IO.IOException>(),
                    OnOpened = args =>
                    {
                        var logger = context.ServiceProvider.GetService<ILoggerFactory>()?
                            .CreateLogger("Resilience.Smtp");
                        logger?.LogWarning("SMTP circuit breaker OPENED for {Duration}s",
                            args.BreakDuration.TotalSeconds);
                        return ValueTask.CompletedTask;
                    }
                });
        });

        return services;
    }
}
