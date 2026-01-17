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

        return services;
    }
}
