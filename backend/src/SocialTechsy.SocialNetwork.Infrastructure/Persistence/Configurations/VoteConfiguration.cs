using SocialTechsy.SocialNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Vote
/// </summary>
public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.ToTable("Votes");

        builder.HasKey(v => v.VoteId);

        builder.Property(v => v.IsUpvote)
            .IsRequired();

        builder.Property(v => v.CreatedDate)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(v => v.QuestionId);
        builder.HasIndex(v => v.AnswerId);
        builder.HasIndex(v => new { v.UserId, v.QuestionId })
            .IsUnique()
            .HasFilter("[QuestionId] IS NOT NULL");
        builder.HasIndex(v => new { v.UserId, v.AnswerId })
            .IsUnique()
            .HasFilter("[AnswerId] IS NOT NULL");

        // Relationships
        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Question)
            .WithMany()
            .HasForeignKey(v => v.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Answer)
            .WithMany()
            .HasForeignKey(v => v.AnswerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
