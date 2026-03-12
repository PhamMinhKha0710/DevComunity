namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface ISocialEventPublisher
{
    Task PublishLikeEventAsync(LikeEvent evt);
}

public class LikeEvent
{
    public string TargetType { get; set; } = "";
    public int TargetId { get; set; }
    public int LikedByUserId { get; set; }
    public string LikedByDisplayName { get; set; } = "";
    public int ContentAuthorId { get; set; }
    public string? ContentTitle { get; set; }
    public int? QuestionId { get; set; }
    public long LikeCount { get; set; }
}
