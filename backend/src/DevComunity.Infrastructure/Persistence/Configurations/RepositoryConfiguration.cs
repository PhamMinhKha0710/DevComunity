using DevComunity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevComunity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Repository
/// </summary>
public class RepositoryConfiguration : IEntityTypeConfiguration<Repository>
{
    public void Configure(EntityTypeBuilder<Repository> builder)
    {
        builder.ToTable("Repositories");

        builder.HasKey(r => r.RepositoryId);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.GiteaRepoId)
            .HasMaxLength(100);

        builder.Property(r => r.CloneUrl)
            .HasMaxLength(500);

        builder.Property(r => r.DefaultBranch)
            .HasMaxLength(50)
            .HasDefaultValue("main");

        builder.Property(r => r.IsPrivate)
            .HasDefaultValue(false);

        builder.Property(r => r.CreatedDate)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(r => r.Name);
        builder.HasIndex(r => r.OwnerId);

        // Relationships - Restrict
        builder.HasOne(r => r.Owner)
            .WithMany()
            .HasForeignKey(r => r.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
