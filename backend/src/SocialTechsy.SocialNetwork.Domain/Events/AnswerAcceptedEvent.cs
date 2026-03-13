namespace SocialTechsy.SocialNetwork.Domain.Events;

public class AnswerAcceptedEvent : DomainEvent
{
    public int AnswerId { get; init; }
    public int QuestionId { get; init; }
    public int AnswerAuthorId { get; init; }
    public int QuestionOwnerId { get; init; }
    public string QuestionTitle { get; init; } = "";
}
