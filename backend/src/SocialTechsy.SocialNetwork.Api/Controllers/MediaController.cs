using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using SocialTechsy.SocialNetwork.Infrastructure.GridFs;
using System.Security.Claims;

using GridFSFileInfo = MongoDB.Driver.GridFS.GridFSFileInfo;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

/// <summary>
/// API Controller for Media/File uploads in chat
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly ILogger<MediaController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IGridFsService _gridFsService;

    // Allowed file types for chat
    private static readonly string[] AllowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedVideoTypes = { ".mp4", ".webm", ".mov" };
    private static readonly string[] AllowedAudioTypes = { ".mp3", ".wav", ".ogg", ".m4a" };
    private static readonly string[] AllowedFileTypes = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip", ".rar" };

    private static readonly Dictionary<string, string> ContentTypeMap = new()
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".webp", "image/webp" },
        { ".mp4", "video/mp4" },
        { ".webm", "video/webm" },
        { ".mov", "video/quicktime" },
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".m4a", "audio/mp4" },
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".txt", "text/plain" },
        { ".zip", "application/zip" },
        { ".rar", "application/x-rar-compressed" }
    };

    private const long MaxFileSize = 25 * 1024 * 1024; // 25 MB

    public MediaController(
        ILogger<MediaController> logger,
        IWebHostEnvironment environment,
        IGridFsService gridFsService)
    {
        _logger = logger;
        _environment = environment;
        _gridFsService = gridFsService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Upload a file for chat message - stored in GridFS
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<UploadResult>> UploadFile(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        if (file.Length > MaxFileSize)
            return BadRequest(new { message = "File size exceeds 25 MB limit" });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var messageType = GetMessageType(extension);

        if (messageType == null)
            return BadRequest(new { message = "File type not allowed" });

        try
        {
            // Determine content type
            var contentType = ContentTypeMap.GetValueOrDefault(extension, "application/octet-stream");

            // Upload to GridFS
            using var stream = file.OpenReadStream();
            var objectId = await _gridFsService.UploadAsync(stream, file.FileName, contentType, userId);

            // Return URL with GridFS ObjectId
            var fileUrl = $"/api/media/{objectId}";

            _logger.LogInformation("User {UserId} uploaded file to GridFS: {FileName} ({Size} bytes), ObjectId: {ObjectId}",
                userId, file.FileName, file.Length, objectId);

            return Ok(new UploadResult
            {
                Url = fileUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                MessageType = messageType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to GridFS for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to upload file" });
        }
    }

    /// <summary>
    /// Upload multiple files at once - stored in GridFS
    /// </summary>
    [HttpPost("upload-multiple")]
    [ProducesResponseType(typeof(List<UploadResult>), StatusCodes.Status200OK)]
    [RequestSizeLimit(MaxFileSize * 5)] // Allow up to 5 files
    public async Task<ActionResult<List<UploadResult>>> UploadMultipleFiles(List<IFormFile> files)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files provided" });

        if (files.Count > 5)
            return BadRequest(new { message = "Maximum 5 files allowed per upload" });

        var results = new List<UploadResult>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;
            if (file.Length > MaxFileSize) continue;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var messageType = GetMessageType(extension);
            if (messageType == null) continue;

            try
            {
                var contentType = ContentTypeMap.GetValueOrDefault(extension, "application/octet-stream");

                using var stream = file.OpenReadStream();
                var objectId = await _gridFsService.UploadAsync(stream, file.FileName, contentType, userId);

                results.Add(new UploadResult
                {
                    Url = $"/api/media/{objectId}",
                    FileName = file.FileName,
                    FileSize = file.Length,
                    MessageType = messageType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to GridFS", file.FileName);
            }
        }

        return Ok(results);
    }

    private string? GetMessageType(string extension)
    {
        if (AllowedImageTypes.Contains(extension)) return "image";
        if (AllowedVideoTypes.Contains(extension)) return "video";
        if (AllowedAudioTypes.Contains(extension)) return "audio";
        if (AllowedFileTypes.Contains(extension)) return "file";
        return null;
    }

    /// <summary>
    /// Get a file from GridFS by ObjectId
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous] // Allow viewing media without auth (chat messages may be viewed by recipients)
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(string id)
    {
        try
        {
            var fileInfo = await _gridFsService.GetFileInfoAsync(id);
            if (fileInfo == null)
                return NotFound(new { message = "File not found" });

            var stream = await _gridFsService.DownloadAsync(id);
            if (stream == null)
                return NotFound(new { message = "File content not found" });

            // Get content type from metadata or infer from filename
            var contentType = "application/octet-stream";
            if (fileInfo.Metadata != null)
            {
                if (fileInfo.Metadata.Contains("contentType"))
                    contentType = fileInfo.Metadata["contentType"].AsString;
                else if (fileInfo.Metadata.Contains("fileName"))
                {
                    var fileName = fileInfo.Metadata["fileName"].AsString;
                    var ext = Path.GetExtension(fileName).ToLowerInvariant();
                    contentType = ContentTypeMap.TryGetValue(ext, out var ct) ? ct : "application/octet-stream";
                }
            }

            // Set cache headers for better performance
            Response.Headers.Append("Cache-Control", "public, max-age=31536000");

            return File(stream, contentType, fileInfo.Filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file {FileId} from GridFS", id);
            return StatusCode(500, new { message = "Error retrieving file" });
        }
    }

    /// <summary>
    /// Delete a file from GridFS
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var fileInfo = await _gridFsService.GetFileInfoAsync(id);
            if (fileInfo == null)
                return NotFound(new { message = "File not found" });

            // Check if user owns the file
            if (fileInfo.Metadata != null && fileInfo.Metadata.Contains("uploadedBy"))
            {
                var uploadedBy = fileInfo.Metadata["uploadedBy"].AsInt32;
                if (uploadedBy != userId)
                    return Forbid();
            }

            var deleted = await _gridFsService.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { message = "Failed to delete file" });

            _logger.LogInformation("User {UserId} deleted file {FileId} from GridFS", userId, id);
            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId} from GridFS", id);
            return StatusCode(500, new { message = "Error deleting file" });
        }
    }
}

public class UploadResult
{
    public string Url { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public long FileSize { get; set; }
    public string MessageType { get; set; } = null!;
}
