namespace SocialTechsy.SocialNetwork.Shared.Constants;

/// <summary>
/// Reputation points awarded for various actions in the system.
/// </summary>
public static class ReputationPoints
{
    public const int QuestionUpvote = 5;
    public const int QuestionDownvote = -2;
    public const int AnswerUpvote = 10;
    public const int AnswerDownvote = -2;
    public const int DownvoteCost = -1;
    public const int AcceptedAnswerAuthor = 15;
    public const int AcceptedAnswerOwner = 2;
    public const int AskQuestion = 2;
}
