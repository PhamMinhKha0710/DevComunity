using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;

namespace SocialTechsy.SocialNetwork.Application.Queries.Search;

/// <summary>
/// DTO for unified search results
/// </summary>
public class SearchResultDto
{
    public List<QuestionSearchResult> Questions { get; set; } = new();
    public List<UserSearchResult> Users { get; set; } = new();
    public List<TagSearchResult> Tags { get; set; } = new();
}

public class QuestionSearchResult
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = null!;
    public int Score { get; set; }
    public int AnswerCount { get; set; }
    public int ViewCount { get; set; }
    public string AuthorName { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class UserSearchResult
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
    public int ReputationPoints { get; set; }
}

public class TagSearchResult
{
    public int TagId { get; set; }
    public string TagName { get; set; } = null!;
    public string? Description { get; set; }
    public int QuestionCount { get; set; }
}
