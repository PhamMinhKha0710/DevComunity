namespace DevComunity.Application.Common.DTOs;

/// <summary>
/// DTO for comment
/// </summary>
public class CommentDto
{
    public int CommentId { get; set; }
    public string Body { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public int AuthorId { get; set; }
    public string AuthorUsername { get; set; } = null!;
    public string? AuthorProfilePicture { get; set; }
}
