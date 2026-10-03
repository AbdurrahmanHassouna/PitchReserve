using MediatR;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetOwnerBookings;

public record GetOwnerBookingsQuery : IRequest<List<OwnerBookingDto>>
{
    public Guid OwnerUserId { get; init; }
    public BookingStatus? Status { get; init; }
    public Guid? PlaygroundId { get; init; }

    public GetOwnerBookingsQuery() { }

    public GetOwnerBookingsQuery(Guid ownerUserId, BookingStatus? status = null, Guid? playgroundId = null)
    {
        OwnerUserId = ownerUserId;
        Status = status;
        PlaygroundId = playgroundId;
    }
}
