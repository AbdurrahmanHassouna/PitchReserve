using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Queries.GetBookingById;

public class GetBookingByIdQueryValidator : AbstractValidator<GetBookingByIdQuery>
{
    public GetBookingByIdQueryValidator()
    {
        RuleFor(query => query.BookingId).NotEmpty();
    }
}
