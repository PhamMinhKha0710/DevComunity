namespace DevComunity.Domain.Entities;

/// <summary>
/// Attachment entity - represents a file attachment
/// </summary>
public class Attachment
{
    public int AttachmentId { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public long FileSize { get; set; }
    public DateTime UploadedDate { get; set; }

    // Foreign keys
    public int UserId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
