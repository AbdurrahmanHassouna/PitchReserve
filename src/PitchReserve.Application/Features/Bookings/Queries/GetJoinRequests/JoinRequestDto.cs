using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetJoinRequests;

public record JoinRequestDto
{
    public Guid ParticipantId { get; init; }
    public Guid PlayerId { get; init; }
    public string PlayerName { get; init; } = string.Empty;
    public ParticipantStatus Status { get; init; }
    public DateTime RequestedAt { get; init; }
}
