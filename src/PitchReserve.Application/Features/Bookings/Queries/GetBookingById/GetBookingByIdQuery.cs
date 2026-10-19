using MediatR;

namespace PitchReserve.Application.Features.Bookings.Queries.GetBookingById;

public record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingDetailsDto>;
