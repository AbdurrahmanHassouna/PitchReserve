using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PitchReserve.Domain.Entities;

namespace PitchReserve.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        builder.HasIndex(r => new { r.PlaygroundId, r.PlayerId })
            .IsUnique();

        builder.HasOne(r => r.Playground)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.PlaygroundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Player)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
