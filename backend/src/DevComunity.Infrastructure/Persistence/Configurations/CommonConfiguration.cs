using DevComunity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevComunity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for SavedItem
/// </summary>
public class SavedItemConfiguration : IEntityTypeConfiguration<SavedItem>
{
    public void Configure(EntityTypeBuilder<SavedItem> builder)
    {
        builder.ToTable("SavedItems");

        builder.HasKey(s => s.SavedItemId);

        builder.Property(s => s.CreatedDate)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes (split into two separate filtered indexes)
        builder.HasIndex(s => new { s.UserId, s.QuestionId }).HasFilter("[QuestionId] IS NOT NULL");
        builder.HasIndex(s => new { s.UserId, s.AnswerId }).HasFilter("[AnswerId] IS NOT NULL");

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Question)
            .WithMany()
            .HasForeignKey(s => s.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Answer)
            .WithMany()
            .HasForeignKey(s => s.AnswerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for Attachment
/// </summary>
public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(a => a.AttachmentId);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.UploadedDate)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
