using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace SocialTechsy.SocialNetwork.Infrastructure.GridFs;

public interface IGridFsService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, int uploadedByUserId);
    Task<GridFSFileInfo?> GetFileInfoAsync(string objectId);
    Task<Stream?> DownloadAsync(string objectId);
    Task<bool> DeleteAsync(string objectId);
}

public class GridFsService : IGridFsService
{
    private readonly GridFSBucket _gridFsBucket;

    public GridFsService(IMongoDatabase database)
    {
        _gridFsBucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = "media",
            ChunkSizeBytes = 255 * 1024 // 255KB chunks
        });
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, int uploadedByUserId)
    {
        var options = new GridFSUploadOptions
        {
            Metadata = new BsonDocument
            {
                { "fileName", fileName },
                { "contentType", contentType },
                { "uploadedBy", uploadedByUserId },
                { "uploadedAt", DateTime.UtcNow }
            }
        };

        var objectId = await _gridFsBucket.UploadFromStreamAsync(fileName, stream, options);
        return objectId.ToString();
    }

    public async Task<GridFSFileInfo?> GetFileInfoAsync(string objectId)
    {
        if (!ObjectId.TryParse(objectId, out var objectIdValue))
            return null;

        var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", objectIdValue);
        var fileInfo = await _gridFsBucket.Find(filter).FirstOrDefaultAsync();
        return fileInfo;
    }

    public async Task<Stream?> DownloadAsync(string objectId)
    {
        if (!ObjectId.TryParse(objectId, out var objectIdValue))
            return null;

        try
        {
            var stream = await _gridFsBucket.OpenDownloadStreamAsync(objectIdValue);
            return stream;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string objectId)
    {
        if (!ObjectId.TryParse(objectId, out var objectIdValue))
            return false;

        try
        {
            await _gridFsBucket.DeleteAsync(objectIdValue);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
