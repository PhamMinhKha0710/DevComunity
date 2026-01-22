using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Application.Interfaces.Services;
using DevComunity.Infrastructure.Persistence.Data;
using DevComunity.Infrastructure.Persistence.Repositories;
using DevComunity.Infrastructure.Services;

namespace DevComunity.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database context
        services.AddDbContext<DevComunityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(DevComunityDbContext).Assembly.FullName)));

        // Register repositories
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IAnswerRepository, AnswerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ISavedItemRepository, SavedItemRepository>();

        // Register services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<DataSeeder>();

        return services;
    }
}
