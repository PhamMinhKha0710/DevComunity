using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.External.Gitea;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;
using SocialTechsy.SocialNetwork.Infrastructure.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure;

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
        services.AddDbContext<SocialTechsySocialNetworkDbContext>(options =>
            options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(SocialTechsySocialNetworkDbContext).Assembly.FullName)
                          .EnableRetryOnFailure(3))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

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
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<ICodeRepository, CodeRepository>();

        // Social Networking repositories
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ITagPreferenceRepository, TagPreferenceRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Register services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<DataSeeder>();
        services.AddSingleton<ICacheService, CacheService>();

        // Gitea integration
        services.Configure<GiteaConfiguration>(
            configuration.GetSection(GiteaConfiguration.SectionName));
        services.AddHttpClient<IGiteaService, GiteaService>();

        return services;
    }
}

