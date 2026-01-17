using DevComunity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevComunity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Comment
/// </summary>
public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.CommentId);

        builder.Property(c => c.Body)
            .IsRequired()
            .HasMaxLength(600);

        builder.Property(c => c.CreatedDate)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(c => c.QuestionId);
        builder.HasIndex(c => c.AnswerId);

        // Relationships
        builder.HasOne(c => c.Question)
            .WithMany(q => q.Comments)
            .HasForeignKey(c => c.QuestionId)
            .OnDelete(DeleteBehavior.Restrict); // Changed to Restrict

        builder.HasOne(c => c.Answer)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.AnswerId)
            .OnDelete(DeleteBehavior.Restrict); // Changed to Restrict

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
