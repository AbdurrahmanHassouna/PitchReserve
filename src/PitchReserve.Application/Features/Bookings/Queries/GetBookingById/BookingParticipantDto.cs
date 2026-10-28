using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetBookingById;

public record BookingParticipantDto
{
    public Guid PlayerId { get; init; }
    public string PlayerName { get; init; } = string.Empty;
    public ParticipantStatus Status { get; init; }
    public DateTime JoinedAt { get; init; }
    public bool IsOrganizer { get; init; }
}
