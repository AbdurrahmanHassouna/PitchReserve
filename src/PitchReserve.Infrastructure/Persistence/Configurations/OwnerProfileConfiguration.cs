using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PitchReserve.Domain.Entities;

namespace PitchReserve.Infrastructure.Persistence.Configurations;

public class OwnerProfileConfiguration : IEntityTypeConfiguration<OwnerProfile>
{
    public void Configure(EntityTypeBuilder<OwnerProfile> builder)
    {
        builder.ToTable("OwnerProfiles");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.HasIndex(o => o.UserId)
            .IsUnique();

        builder.Property(o => o.BusinessName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.TaxNumber)
            .HasMaxLength(50);

        builder.Property(o => o.StripeConnectedAccountId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(o => o.MaskedIBAN)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.HasOne(o => o.User)
            .WithOne(u => u.OwnerProfile)
            .HasForeignKey<OwnerProfile>(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Playgrounds)
            .WithOne(p => p.OwnerProfile)
            .HasForeignKey(p => p.OwnerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
