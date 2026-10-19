using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetPublicMatches;

public record PublicMatchDto
{
    public Guid BookingId { get; init; }
    public Guid PlaygroundId { get; init; }
    public string PlaygroundName { get; init; } = string.Empty;
    public string LocationCity { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public BookingStatus Status { get; init; }
    public int MaxPlayersNeeded { get; init; }
    public int JoinedParticipantsCount { get; init; }
    public int AvailablePlaces { get; init; }
    public ParticipantStatus? MyParticipantStatus { get; init; }
    public bool CanRequestToJoin { get; init; }
    public bool CanAcceptParticipant { get; init; }
}
