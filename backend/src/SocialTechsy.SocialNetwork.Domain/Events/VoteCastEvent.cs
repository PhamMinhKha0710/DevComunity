namespace SocialTechsy.SocialNetwork.Domain.Events;

public class VoteCastEvent : DomainEvent
{
    public int VoterId { get; init; }
    public int ContentAuthorId { get; init; }
    public int? QuestionId { get; init; }
    public int? AnswerId { get; init; }
    public bool IsUpvote { get; init; }
    public bool IsNewVote { get; init; }
    public bool IsDirectionChange { get; init; }
    public bool WasPreviouslyUpvote { get; init; }
    public string ContentTitle { get; init; } = "";
    public string VoterDisplayName { get; init; } = "";
}
