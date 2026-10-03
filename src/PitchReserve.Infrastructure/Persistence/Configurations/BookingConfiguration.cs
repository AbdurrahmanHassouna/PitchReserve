using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PitchReserve.Domain.Entities;

namespace PitchReserve.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings",table => table.HasCheckConstraint(
            "CK_Bookings_ParticipantCapacity",
            """
            [JoinedParticipantsCount] >= 0
            AND (
                [MaxPlayersNeeded] IS NULL
                OR [JoinedParticipantsCount] <= [MaxPlayersNeeded]
            )
            """));

        builder.HasKey(b => b.Id);

        builder.Property(b => b.StartTime)
            .IsRequired();

        builder.Property(b => b.EndTime)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.IsOpenForPublicPlayers)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(b => b.JoinedParticipantsCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(b => b.Playground)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.PlaygroundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.OrganizerPlayer)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.OrganizerPlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Payment)
            .WithOne(p => p.Booking)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.MatchParticipants)
            .WithOne(m => m.Booking)
            .HasForeignKey(m => m.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
