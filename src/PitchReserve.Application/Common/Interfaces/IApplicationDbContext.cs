using Microsoft.EntityFrameworkCore;
using PitchReserve.Domain.Entities;

namespace PitchReserve.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<OwnerProfile> OwnerProfiles { get; }
    DbSet<Playground> Playgrounds { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<MatchParticipant> MatchParticipants { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Review> Reviews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
