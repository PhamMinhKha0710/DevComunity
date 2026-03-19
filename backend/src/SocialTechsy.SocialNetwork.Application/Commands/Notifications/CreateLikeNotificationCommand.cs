using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Notifications;

public class CreateLikeNotificationCommand : IRequest
{
    public string EventId { get; set; } = null!;
    public string TargetType { get; set; } = null!; // "question" or "answer"
    public int TargetId { get; set; }
    public int LikedByUserId { get; set; }
    public string LikedByDisplayName { get; set; } = null!;
    public int ContentAuthorId { get; set; }
    public string ContentTitle { get; set; } = null!;
    public int QuestionId { get; set; }
    public long LikeCount { get; set; }
}
