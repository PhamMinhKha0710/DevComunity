using DevComunity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevComunity.Infrastructure.Persistence.Configurations;

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

        // Indexes
        builder.HasIndex(q => q.CreatedDate);
        builder.HasIndex(q => q.Score);
        builder.HasIndex(q => q.ViewCount);

        // Full-text search index (optional)
        // builder.HasIndex(q => q.Title).HasMethod("GIN");

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
