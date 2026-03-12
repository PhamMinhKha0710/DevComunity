using SocialTechsy.SocialNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Answer
/// </summary>
public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("Answers");

        builder.HasKey(a => a.AnswerId);

        builder.Property(a => a.Body)
            .IsRequired();

        builder.Property(a => a.Score)
            .HasDefaultValue(0);

        builder.Property(a => a.IsAccepted)
            .HasDefaultValue(false);

        builder.Property(a => a.CreatedDate)
            .HasDefaultValueSql("GETUTCDATE()");

        // Composite: sorted answers for a question (WHERE QuestionId = X ORDER BY Score DESC)
        builder.HasIndex(a => new { a.QuestionId, a.Score })
            .IsDescending(false, true);
        builder.HasIndex(a => new { a.QuestionId, a.IsAccepted });

        // Relationships
        builder.HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany(u => u.Answers)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing for nested replies
        builder.HasOne(a => a.ParentAnswer)
            .WithMany(a => a.ChildAnswers)
            .HasForeignKey(a => a.ParentAnswerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Comments)
            .WithOne(c => c.Answer)
            .HasForeignKey(c => c.AnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
