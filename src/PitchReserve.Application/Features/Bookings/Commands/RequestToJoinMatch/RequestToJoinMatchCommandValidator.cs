using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Commands.RequestToJoinMatch;

public class RequestToJoinMatchCommandValidator : AbstractValidator<RequestToJoinMatchCommand>
{
    public RequestToJoinMatchCommandValidator()
    {
        RuleFor(v => v.BookingId)
            .NotEmpty().WithMessage("Booking ID is required.");

    }
}
