using MediatR;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Commands.CreateBookingByOwner;

public record CreateBookingByOwnerCommand : IRequest<Guid>
{
    public Guid PlaygroundId { get; init; }
    public Guid OrganizerPlayerId { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public bool IsOpenForPublicPlayers { get; init; }
    public int? MaxPlayersNeeded { get; init; }
}
