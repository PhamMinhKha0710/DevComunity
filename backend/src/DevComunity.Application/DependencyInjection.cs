using Microsoft.Extensions.DependencyInjection;

// Question handlers
using DevComunity.Application.CommandHandlers.Questions;
using DevComunity.Application.QueryHandlers.Questions;

// Auth handlers
using DevComunity.Application.CommandHandlers.Auth;

// Answer handlers
using DevComunity.Application.CommandHandlers.Answers;
using DevComunity.Application.QueryHandlers.Answers;

// User handlers
using DevComunity.Application.QueryHandlers.Users;

// Comment handlers
using DevComunity.Application.CommandHandlers.Comments;

// Tag handlers
using DevComunity.Application.QueryHandlers.Tags;

// Notification handlers
using DevComunity.Application.CommandHandlers.Notifications;
using DevComunity.Application.QueryHandlers.Notifications;

// Vote handlers
using DevComunity.Application.CommandHandlers.Votes;

namespace DevComunity.Application;

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

        return services;
    }
}

