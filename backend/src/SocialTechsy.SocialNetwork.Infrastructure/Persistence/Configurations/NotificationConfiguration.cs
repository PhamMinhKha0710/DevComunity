using SocialTechsy.SocialNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Notification
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.NotificationId);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Link)
            .HasMaxLength(500);

        builder.Property(n => n.IsRead)
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedDate)
            .HasDefaultValueSql("GETUTCDATE()");

        // Composite index covering the primary query: WHERE UserId = X AND IsRead = false ORDER BY CreatedDate DESC
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedDate })
            .IsDescending(false, false, true);

        builder.HasIndex(n => new { n.UserId, n.CreatedDate })
            .IsDescending(false, true);

        // Relationships - All Restrict
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.FromUser)
            .WithMany()
            .HasForeignKey(n => n.FromUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
