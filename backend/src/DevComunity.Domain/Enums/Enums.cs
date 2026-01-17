namespace DevComunity.Domain.Enums;

/// <summary>
/// Type of entity that a vote targets
/// </summary>
public enum VoteTargetType
{
    Question,
    Answer
}

/// <summary>
/// Type of notification
/// </summary>
public enum NotificationType
{
    QuestionAnswered,
    AnswerAccepted,
    CommentReceived,
    Upvote,
    Downvote,
    BadgeEarned,
    Mention,
    NewFollower,
    QuestionEdited,
    AnswerEdited
}

/// <summary>
/// Type of badge
/// </summary>
public enum BadgeType
{
    // Question badges
    FirstQuestion,
    QuestionMaster,
    PopularQuestion,
    NotableQuestion,
    FamousQuestion,

    // Answer badges
    FirstAnswer,
    AnswerMaster,
    HelpfulAnswer,
    GreatAnswer,
    AcceptedAnswer,

    // Voting badges
    Voter,
    Critic,
    Supporter,

    // Engagement badges
    Commentator,
    Editor,
    Autobiographer,

    // Special badges
    Beta,
    Yearling
}

/// <summary>
/// Status of a repository
/// </summary>
public enum RepositoryStatus
{
    Active,
    Archived,
    Deleted
}

/// <summary>
/// Type of attachment
/// </summary>
public enum AttachmentType
{
    Image,
    Video,
    Document,
    Other
}
