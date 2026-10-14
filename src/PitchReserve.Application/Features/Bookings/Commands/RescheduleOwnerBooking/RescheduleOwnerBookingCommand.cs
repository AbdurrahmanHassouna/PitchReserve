using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Bookings.Commands.RescheduleOwnerBooking;

public record RescheduleOwnerBookingCommand : IRequest<Result>
{
    public Guid BookingId { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
}
