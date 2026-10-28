using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Bookings.Commands.AcceptCashBooking;

public record AcceptCashBookingCommand(Guid BookingId) : IRequest<Result>;
