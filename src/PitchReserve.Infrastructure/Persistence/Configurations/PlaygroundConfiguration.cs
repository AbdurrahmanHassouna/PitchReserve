using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PitchReserve.Domain.Entities;

namespace PitchReserve.Infrastructure.Persistence.Configurations;

public class PlaygroundConfiguration : IEntityTypeConfiguration<Playground>
{
    public void Configure(EntityTypeBuilder<Playground> builder)
    {
        builder.ToTable("Playgrounds");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.LocationCity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Address)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(p => p.HourlyRate)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.Latitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired(false);

        builder.Property(p => p.Longitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired(false);

        builder.Property(p => p.AverageRating)
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(p => p.TotalRatings)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(p => p.Size)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.SurfaceType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.OpenHour)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(p => p.CloseHour)
            .HasColumnType("time")
            .IsRequired();

        builder.HasOne(p => p.OwnerProfile)
            .WithMany(o => o.Playgrounds)
            .HasForeignKey(p => p.OwnerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Bookings)
            .WithOne(b => b.Playground)
            .HasForeignKey(b => b.PlaygroundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Reviews)
            .WithOne(r => r.Playground)
            .HasForeignKey(r => r.PlaygroundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
