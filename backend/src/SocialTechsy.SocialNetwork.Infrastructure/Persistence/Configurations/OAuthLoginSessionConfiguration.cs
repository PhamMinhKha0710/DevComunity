using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Configurations;

public class OAuthLoginSessionConfiguration : IEntityTypeConfiguration<OAuthLoginSession>
{
    public void Configure(EntityTypeBuilder<OAuthLoginSession> builder)
    {
        builder.ToTable("OAuthLoginSessions");

        builder.HasKey(s => s.OAuthLoginSessionId);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(s => s.Provider)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(s => s.AccessToken)
            .IsRequired();

        builder.Property(s => s.RefreshToken)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.HasIndex(s => s.ExpiresAt);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.IsExpired);
        builder.Ignore(s => s.IsValid);
    }
}
