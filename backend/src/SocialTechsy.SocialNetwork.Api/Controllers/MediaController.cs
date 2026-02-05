using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    
    // Allowed file types for chat
    private static readonly string[] AllowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedVideoTypes = { ".mp4", ".webm", ".mov" };
    private static readonly string[] AllowedAudioTypes = { ".mp3", ".wav", ".ogg", ".m4a" };
    private static readonly string[] AllowedFileTypes = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip", ".rar" };
    
    private const long MaxFileSize = 25 * 1024 * 1024; // 25 MB

    public MediaController(ILogger<MediaController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Upload a file for chat message
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
            // Create uploads directory if not exists
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "chat", userId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Generate URL
            var fileUrl = $"/uploads/chat/{userId}/{uniqueFileName}";

            _logger.LogInformation("User {UserId} uploaded file: {FileName} ({Size} bytes)", 
                userId, file.FileName, file.Length);

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
            _logger.LogError(ex, "Error uploading file for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to upload file" });
        }
    }

    /// <summary>
    /// Upload multiple files at once
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
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "chat", userId.ToString());
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                results.Add(new UploadResult
                {
                    Url = $"/uploads/chat/{userId}/{uniqueFileName}",
                    FileName = file.FileName,
                    FileSize = file.Length,
                    MessageType = messageType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
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
}

public class UploadResult
{
    public string Url { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public long FileSize { get; set; }
    public string MessageType { get; set; } = null!;
}
