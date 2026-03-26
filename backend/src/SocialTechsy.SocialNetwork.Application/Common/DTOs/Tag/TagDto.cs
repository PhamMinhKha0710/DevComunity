namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Tag;

/// <summary>
/// DTO for tag information
/// </summary>
public class TagDto
{
    public int TagId { get; set; }
    public string TagName { get; set; } = null!;
    public string? Description { get; set; }
    public int UsageCount { get; set; }
    public int QuestionCount { get; set; }
}

