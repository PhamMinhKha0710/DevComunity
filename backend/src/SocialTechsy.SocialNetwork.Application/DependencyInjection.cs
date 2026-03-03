using Microsoft.Extensions.DependencyInjection;

// Question handlers
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Questions;

// Auth handlers
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

// Answer handlers
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Answers;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Answers;

// User handlers
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Users;

// Comment handlers
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Comments;

// Tag handlers
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Tags;

// Notification handlers
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Notifications;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Notifications;

// Vote handlers
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Votes;

// SavedItems handlers
using SocialTechsy.SocialNetwork.Application.CommandHandlers.SavedItems;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.SavedItems;

namespace SocialTechsy.SocialNetwork.Application;

/// <summary>
/// Extension methods for registering Application services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Question Command Handlers
        services.AddScoped<CreateQuestionCommandHandler>();
        services.AddScoped<UpdateQuestionCommandHandler>();
        services.AddScoped<DeleteQuestionCommandHandler>();

        // Question Query Handlers
        services.AddScoped<GetQuestionsQueryHandler>();
        services.AddScoped<GetQuestionByIdQueryHandler>();

        // Auth Command Handlers
        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<ForgotPasswordCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();

        // Answer Command Handlers
        services.AddScoped<CreateAnswerCommandHandler>();
        services.AddScoped<UpdateAnswerCommandHandler>();
        services.AddScoped<DeleteAnswerCommandHandler>();
        services.AddScoped<AcceptAnswerCommandHandler>();

        // Answer Query Handlers
        services.AddScoped<GetAnswersByQuestionQueryHandler>();
        services.AddScoped<GetAnswerByIdQueryHandler>();

        // User Query Handlers
        services.AddScoped<GetCurrentUserQueryHandler>();
        services.AddScoped<GetUserByIdQueryHandler>();
        services.AddScoped<GetUsersQueryHandler>();
        services.AddScoped<GetUserQuestionsQueryHandler>();
        services.AddScoped<GetUserAnswersQueryHandler>();

        // Comment Command Handlers
        services.AddScoped<CreateQuestionCommentCommandHandler>();
        services.AddScoped<CreateAnswerCommentCommandHandler>();
        services.AddScoped<UpdateCommentCommandHandler>();
        services.AddScoped<DeleteCommentCommandHandler>();

        // Tag Query Handlers
        services.AddScoped<GetTagsQueryHandler>();
        services.AddScoped<GetTagByNameQueryHandler>();

        // Notification Command Handlers
        services.AddScoped<MarkNotificationReadCommandHandler>();
        services.AddScoped<MarkAllNotificationsReadCommandHandler>();
        services.AddScoped<DeleteNotificationCommandHandler>();

        // Notification Query Handlers
        services.AddScoped<GetNotificationsQueryHandler>();
        services.AddScoped<GetUnreadCountQueryHandler>();

        // Vote Command Handlers
        services.AddScoped<VoteQuestionCommandHandler>();
        services.AddScoped<VoteAnswerCommandHandler>();
        services.AddScoped<RemoveVoteCommandHandler>();

        // SavedItems Command Handlers
        services.AddScoped<SaveQuestionCommandHandler>();
        services.AddScoped<UnsaveQuestionCommandHandler>();
        services.AddScoped<SaveAnswerCommandHandler>();
        services.AddScoped<UnsaveAnswerCommandHandler>();

        // SavedItems Query Handlers
        services.AddScoped<GetSavedItemsQueryHandler>();

        // Search Query Handlers
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Search.SearchQueryHandler>();

        // Badge Query Handlers
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Badges.GetBadgesQueryHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Badges.GetBadgeByIdQueryHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Badges.GetBadgeUsersQueryHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Badges.GetUserBadgesQueryHandler>();

        // Chat Query Handlers
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Chat.GetConversationsQueryHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Chat.GetConversationByIdQueryHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Chat.GetMessagesQueryHandler>();

        // Chat Command Handlers
        services.AddScoped<SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat.StartConversationCommandHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat.SendMessageCommandHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat.MarkConversationReadCommandHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat.AddReactionCommandHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat.RemoveReactionCommandHandler>();

        // Repository Query Handlers
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Repositories.GetRepositoriesQueryHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Repositories.GetRepositoryByIdQueryHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.QueryHandlers.Repositories.GetUserRepositoriesQueryHandler>();

        // Repository Command Handlers
        services.AddScoped<SocialTechsy.SocialNetwork.Application.CommandHandlers.Repositories.CreateRepositoryCommandHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.CommandHandlers.Repositories.UpdateRepositoryCommandHandler>();
        services.AddScoped<SocialTechsy.SocialNetwork.Application.CommandHandlers.Repositories.DeleteRepositoryCommandHandler>();

        return services;
    }
}

