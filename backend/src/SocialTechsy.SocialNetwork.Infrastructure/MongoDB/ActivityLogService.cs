using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.MongoDB;

public class ActivityLogService : IActivityLogService
{
    private readonly IMongoCollection<LikeLog> _likes;
    private readonly IMongoCollection<ViewLog> _views;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(IMongoDatabase database, ILogger<ActivityLogService> logger)
    {
        _likes = database.GetCollection<LikeLog>("post_likes");
        _views = database.GetCollection<ViewLog>("post_views");
        _logger = logger;

        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        _likes.Indexes.CreateOne(new CreateIndexModel<LikeLog>(
            Builders<LikeLog>.IndexKeys
                .Ascending(l => l.TargetType)
                .Ascending(l => l.TargetId)
                .Ascending(l => l.UserId),
            new CreateIndexOptions { Unique = true, Background = true }));

        _views.Indexes.CreateOne(new CreateIndexModel<ViewLog>(
            Builders<ViewLog>.IndexKeys
                .Ascending(v => v.TargetId)
                .Descending(v => v.CreatedAt),
            new CreateIndexOptions { Background = true }));
    }

    public async Task LogLikeAsync(string targetType, int targetId, int userId)
    {
        try
        {
            var filter = Builders<LikeLog>.Filter.And(
                Builders<LikeLog>.Filter.Eq(l => l.TargetType, targetType),
                Builders<LikeLog>.Filter.Eq(l => l.TargetId, targetId),
                Builders<LikeLog>.Filter.Eq(l => l.UserId, userId));

            var update = Builders<LikeLog>.Update
                .SetOnInsert(l => l.CreatedAt, DateTime.UtcNow)
                .SetOnInsert(l => l.TargetType, targetType)
                .SetOnInsert(l => l.TargetId, targetId)
                .SetOnInsert(l => l.UserId, userId);

            await _likes.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log like activity: {TargetType}:{TargetId} by {UserId}", targetType, targetId, userId);
        }
    }

    public async Task LogUnlikeAsync(string targetType, int targetId, int userId)
    {
        try
        {
            var filter = Builders<LikeLog>.Filter.And(
                Builders<LikeLog>.Filter.Eq(l => l.TargetType, targetType),
                Builders<LikeLog>.Filter.Eq(l => l.TargetId, targetId),
                Builders<LikeLog>.Filter.Eq(l => l.UserId, userId));

            await _likes.DeleteOneAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log unlike activity: {TargetType}:{TargetId} by {UserId}", targetType, targetId, userId);
        }
    }

    public async Task LogViewAsync(int questionId, int? userId, string? ip, string? userAgent)
    {
        try
        {
            await _views.InsertOneAsync(new ViewLog
            {
                TargetId = questionId,
                UserId = userId,
                Ip = ip,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log view activity: question {QuestionId}", questionId);
        }
    }
}

public class LikeLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string TargetType { get; set; } = "";
    public int TargetId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ViewLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int TargetId { get; set; }
    public int? UserId { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}
