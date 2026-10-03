using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Bookings.Commands.RequestToJoinMatch;

public record RequestToJoinMatchCommand : IRequest<Result<Guid>>
{
    public Guid BookingId { get; init; }
    public Guid PlayerId { get; init; }

    public RequestToJoinMatchCommand() { }

    public RequestToJoinMatchCommand(Guid bookingId, Guid playerId)
    {
        BookingId = bookingId;
        PlayerId = playerId;
    }
}
