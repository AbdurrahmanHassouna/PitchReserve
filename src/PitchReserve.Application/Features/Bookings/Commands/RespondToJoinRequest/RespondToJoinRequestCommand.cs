using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Bookings.Commands.RespondToJoinRequest;

public record RespondToJoinRequestCommand : IRequest<Result>
{
    public Guid BookingId { get; init; }
    public Guid ParticipantId { get; init; }
    public bool Accept { get; init; }

    public RespondToJoinRequestCommand() { }

    public RespondToJoinRequestCommand(Guid bookingId, Guid participantId, bool accept)
    {
        BookingId = bookingId;
        ParticipantId = participantId;
        Accept = accept;
    }
}
