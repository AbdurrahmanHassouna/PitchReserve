using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Bookings.Commands.CancelBooking;

public record CancelBookingCommand : IRequest<Result>
{
    public Guid BookingId { get; init; }
    public string? Reason { get; init; }
}
