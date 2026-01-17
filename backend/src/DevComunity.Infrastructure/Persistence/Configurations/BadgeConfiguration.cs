using DevComunity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevComunity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Badge
/// </summary>
public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("Badges");

        builder.HasKey(b => b.BadgeId);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Description)
            .HasMaxLength(500);

        builder.Property(b => b.IconUrl)
            .HasMaxLength(500);

        builder.Property(b => b.BadgeType)
            .IsRequired()
            .HasMaxLength(20);

        // Indexes
        builder.HasIndex(b => b.Name)
            .IsUnique();

        builder.HasIndex(b => b.BadgeType);
    }
}

/// <summary>
/// Entity configuration for UserBadge
/// </summary>
public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("UserBadges");

        builder.HasKey(ub => ub.UserBadgeId);

        builder.Property(ub => ub.EarnedDate)
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique constraint: User can only earn a badge once
        builder.HasIndex(ub => new { ub.UserId, ub.BadgeId })
            .IsUnique();

        // Relationships - All Restrict
        builder.HasOne(ub => ub.User)
            .WithMany(u => u.Badges)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ub => ub.Badge)
            .WithMany()
            .HasForeignKey(ub => ub.BadgeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
