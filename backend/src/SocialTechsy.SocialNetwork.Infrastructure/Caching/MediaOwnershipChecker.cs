using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.GridFs;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class MediaOwnershipChecker : IMediaOwnershipChecker
{
    private readonly IGridFsService _gridFsService;

    public MediaOwnershipChecker(IGridFsService gridFsService)
    {
        _gridFsService = gridFsService;
    }

    public async Task<bool> IsOwnerAsync(string mediaUrl, int userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mediaUrl))
            return false;

        if (!mediaUrl.StartsWith("/api/media/", StringComparison.OrdinalIgnoreCase))
            return false;

        var objectId = mediaUrl.Substring("/api/media/".Length);
        var fileInfo = await _gridFsService.GetFileInfoAsync(objectId);

        if (fileInfo == null || fileInfo.Metadata == null)
            return false;

        if (fileInfo.Metadata.TryGetValue("uploadedBy", out var uploadedByElement) &&
            uploadedByElement.IsInt32 &&
            uploadedByElement.AsInt32 == userId)
        {
            return true;
        }

        return false;
    }
}
