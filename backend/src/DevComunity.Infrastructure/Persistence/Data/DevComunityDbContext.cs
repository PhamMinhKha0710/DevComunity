using DevComunity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Data;

/// <summary>
/// Entity Framework Core DbContext for DevComunity
/// Uses separate IEntityTypeConfiguration files for each entity
/// </summary>
public class DevComunityDbContext : DbContext
{
    public DevComunityDbContext(DbContextOptions<DevComunityDbContext> options) 
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

    // Saved Items
    public DbSet<SavedItem> SavedItems => Set<SavedItem>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DevComunityDbContext).Assembly);
    }
}
