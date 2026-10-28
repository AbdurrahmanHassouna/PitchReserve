using MediatR;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetOwnerBookings;

public record GetOwnerBookingsQuery : IRequest<List<OwnerBookingDto>>
{
    public BookingStatus? Status { get; init; }
    public Guid? PlaygroundId { get; init; }
}
