using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetMyBookings;

public record MyBookingDto
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
    public bool IsOpenForPublicPlayers { get; init; }
    public int? MaxPlayersNeeded { get; init; }
    public int JoinedParticipantsCount { get; init; }
    public int? AvailablePlaces { get; init; }
    public ParticipantStatus? MyParticipantStatus { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public decimal? PaymentAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}
