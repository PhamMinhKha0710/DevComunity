namespace SocialTechsy.SocialNetwork.Application.Common.DTOs;

/// <summary>
/// DTO for tag preference
/// </summary>
public class TagPreferenceDto
{
    public int TagId { get; set; }
    public string TagName { get; set; } = "";
    public bool IsFollowed { get; set; }
    public bool IsIgnored { get; set; }
}
