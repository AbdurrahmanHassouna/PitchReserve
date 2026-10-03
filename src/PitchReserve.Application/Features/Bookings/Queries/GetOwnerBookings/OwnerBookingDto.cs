using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetOwnerBookings;

public record OwnerBookingDto
{
    public Guid BookingId { get; init; }
    public Guid PlaygroundId { get; init; }
    public string PlaygroundName { get; init; } = string.Empty;
    public Guid OrganizerPlayerId { get; init; }
    public string OrganizerPlayerName { get; init; } = string.Empty;
    public string? OrganizerPhoneNumber { get; init; }
    public DateTime BookingTime { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public BookingStatus Status { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public decimal? PaymentAmount { get; init; }
    public int TotalParticipants { get; init; }
    public int JoinedParticipants { get; init; }
    public int PendingParticipants { get; init; }
    public DateTime CreatedAt { get; init; }
}
