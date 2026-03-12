using SocialTechsy.SocialNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Question
/// </summary>
public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");

        builder.HasKey(q => q.QuestionId);

        builder.Property(q => q.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(q => q.Body)
            .IsRequired();

        builder.Property(q => q.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("open");

        builder.Property(q => q.ViewCount)
            .HasDefaultValue(0);

        builder.Property(q => q.Score)
            .HasDefaultValue(0);

        builder.Property(q => q.CreatedDate)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(q => q.CreatedDate)
            .IsDescending(true);
        builder.HasIndex(q => q.Score)
            .IsDescending(true);
        builder.HasIndex(q => q.ViewCount);
        builder.HasIndex(q => q.Status);

        // Composite: user profile question list (WHERE UserId = X ORDER BY CreatedDate DESC)
        builder.HasIndex(q => new { q.UserId, q.CreatedDate })
            .IsDescending(false, true);

        // Relationships
        builder.HasOne(q => q.User)
            .WithMany(u => u.Questions)
            .HasForeignKey(q => q.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict); // Changed to Restrict to avoid cycles

        builder.HasMany(q => q.Comments)
            .WithOne(c => c.Question)
            .HasForeignKey(c => c.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(q => q.QuestionTags)
            .WithOne(qt => qt.Question)
            .HasForeignKey(qt => qt.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
