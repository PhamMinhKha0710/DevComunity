using SocialTechsy.SocialNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

/// <summary>
/// Entity Framework Core DbContext for SocialTechsy.SocialNetwork
/// Uses separate IEntityTypeConfiguration files for each entity
/// </summary>
public class SocialTechsySocialNetworkDbContext : DbContext
{
    public SocialTechsySocialNetworkDbContext(DbContextOptions<SocialTechsySocialNetworkDbContext> options) 
        : base(options)
    {
    }

    // Q&A Core
    public DbSet<User> Users => Set<User>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<QuestionTag> QuestionTags => Set<QuestionTag>();
    public DbSet<Vote> Votes => Set<Vote>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    // Badges & Gamification
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    // Repository/Code
    public DbSet<Repository> Repositories => Set<Repository>();

    // Chat/Messaging
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();

    // Saved Items
    public DbSet<SavedItem> SavedItems => Set<SavedItem>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    // Social Networking
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Post> Posts => Set<Post>();
    
    // Tag Preferences
    public DbSet<TagPreference> TagPreferences => Set<TagPreference>();

    // Auth Tokens
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialTechsySocialNetworkDbContext).Assembly);
    }
}
