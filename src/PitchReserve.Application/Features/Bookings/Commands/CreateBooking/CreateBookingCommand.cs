using MediatR;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Commands.CreateBooking;

public record CreateBookingCommand : IRequest<Guid>
{
    public Guid PlaygroundId { get; init; }
    public Guid OrganizerPlayerId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public bool IsOpenForPublicPlayers { get; init; }
    public int? MaxPlayersNeeded { get; init; }
}
