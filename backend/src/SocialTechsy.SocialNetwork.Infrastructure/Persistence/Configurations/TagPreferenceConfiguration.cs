using SocialTechsy.SocialNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for TagPreference entity
/// </summary>
public class TagPreferenceConfiguration : IEntityTypeConfiguration<TagPreference>
{
    public void Configure(EntityTypeBuilder<TagPreference> builder)
    {
        builder.ToTable("TagPreferences");

        builder.HasKey(tp => tp.TagPreferenceId);

        builder.Property(tp => tp.IsFollowed)
            .HasDefaultValue(false);

        builder.Property(tp => tp.IsIgnored)
            .HasDefaultValue(false);

        builder.Property(tp => tp.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique constraint: one preference per user-tag combination
        builder.HasIndex(tp => new { tp.UserId, tp.TagId })
            .IsUnique();

        // Index for querying followed tags by user
        builder.HasIndex(tp => new { tp.UserId, tp.IsFollowed })
            .HasDatabaseName("IX_TagPreferences_UserId_IsFollowed")
            .HasFilter("[IsFollowed] = 1");

        // Relationships
        builder.HasOne(tp => tp.User)
            .WithMany()
            .HasForeignKey(tp => tp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tp => tp.Tag)
            .WithMany()
            .HasForeignKey(tp => tp.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
