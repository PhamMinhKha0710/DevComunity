namespace SocialTechsy.SocialNetwork.Shared.Constants;

/// <summary>
/// Validation constants used across the application.
/// </summary>
public static class ValidationConstants
{
    // Username
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 50;

    // Password
    public const int PasswordMinLength = 6;

    // Display / Profile
    public const int DisplayNameMaxLength = 100;
    public const int BioMaxLength = 500;
    public const int LocationMaxLength = 100;
    public const int WebsiteMaxLength = 200;

    // Q&A Content
    public const int QuestionTitleMinLength = 10;
    public const int QuestionTitleMaxLength = 500;
    public const int QuestionBodyMinLength = 30;
    public const int AnswerBodyMinLength = 30;
}
