using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Infrastructure.Caching;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that periodically syncs view count deltas from Redis to SQL Server.
/// Uses a tracking Set instead of SCAN to avoid O(N) key enumeration.
/// </summary>
public class ViewSyncBackgroundService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ViewSyncBackgroundService> _logger;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);

    public ViewSyncBackgroundService(
        IConnectionMultiplexer redis,
        IServiceProvider serviceProvider,
        ILogger<ViewSyncBackgroundService> logger)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ViewSyncBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(SyncInterval, stoppingToken);
                await SyncViewCountsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing view counts from Redis to SQL");
            }
        }

        _logger.LogInformation("ViewSyncBackgroundService stopped");
    }

    private async Task SyncViewCountsAsync(CancellationToken ct)
    {
        var viewService = _serviceProvider.GetRequiredService<RedisViewService>();
        var dirtyIds = await viewService.GetDirtyQuestionIdsAsync();

        if (dirtyIds.Length == 0) return;

        using var scope = _serviceProvider.CreateScope();
        var questionRepo = scope.ServiceProvider.GetRequiredService<IQuestionRepository>();
        var db = _redis.GetDatabase();
        var synced = 0;

        foreach (var questionId in dirtyIds)
        {
            try
            {
                var deltaKey = $"view:question:{questionId}:delta";
                var pendingKey = $"view:question:{questionId}:delta:pending";

                var delta = await db.StringGetSetAsync(deltaKey, 0);
                if (!delta.HasValue || (long)delta == 0)
                {
                    var leftover = await db.StringGetAsync(pendingKey);
                    if (!leftover.HasValue || (long)leftover == 0)
                    {
                        await viewService.RemoveFromTrackingAsync(questionId);
                        continue;
                    }
                    delta = leftover;
                }
                else
                {
                    var existing = await db.StringGetAsync(pendingKey);
                    var total = (long)delta + (existing.HasValue ? (long)existing : 0);
                    await db.StringSetAsync(pendingKey, total);
                    delta = total;
                }

                await questionRepo.IncrementViewCountByDeltaAsync(questionId, (long)delta, ct);
                await db.KeyDeleteAsync(pendingKey);
                await viewService.RemoveFromTrackingAsync(questionId);
                synced++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync view delta for question {QuestionId} (pending delta preserved)", questionId);
            }
        }

        if (synced > 0)
            _logger.LogInformation("Synced view counts for {Count} questions", synced);
    }
}
