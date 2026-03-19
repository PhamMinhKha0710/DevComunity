using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

namespace SocialTechsy.SocialNetwork.Infrastructure.DependencyInjection;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<OutboxSaveChangesInterceptor>();
        services.AddDbContext<SocialTechsySocialNetworkDbContext>((sp, options) =>
            options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(SocialTechsySocialNetworkDbContext).Assembly.FullName)
                          .EnableRetryOnFailure(3))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>()));

        var fullTextEnabled = configuration.GetValue<bool>("Search:FullTextEnabled");
        services.AddScoped<IQuestionRepository>(sp =>
            new QuestionRepository(
                sp.GetRequiredService<SocialTechsySocialNetworkDbContext>(),
                fullTextEnabled));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAnswerRepository, AnswerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ISavedItemRepository, SavedItemRepository>();
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<ICodeRepository, CodeRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ITagPreferenceRepository, TagPreferenceRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOAuthLoginSessionRepository, OAuthLoginSessionRepository>();

        return services;
    }
}
