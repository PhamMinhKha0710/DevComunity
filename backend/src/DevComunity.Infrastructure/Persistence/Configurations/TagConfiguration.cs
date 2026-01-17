using DevComunity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevComunity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Tag
/// </summary>
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.TagId);

        builder.Property(t => t.TagName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.UsageCount)
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(t => t.TagName)
            .IsUnique();

        builder.HasIndex(t => t.UsageCount);
    }
}

/// <summary>
/// Entity configuration for QuestionTag (many-to-many join table)
/// </summary>
public class QuestionTagConfiguration : IEntityTypeConfiguration<QuestionTag>
{
    public void Configure(EntityTypeBuilder<QuestionTag> builder)
    {
        builder.ToTable("QuestionTags");

        // Composite key
        builder.HasKey(qt => new { qt.QuestionId, qt.TagId });

        // Relationships - All Restrict
        builder.HasOne(qt => qt.Question)
            .WithMany(q => q.QuestionTags)
            .HasForeignKey(qt => qt.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(qt => qt.Tag)
            .WithMany(t => t.QuestionTags)
            .HasForeignKey(qt => qt.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
