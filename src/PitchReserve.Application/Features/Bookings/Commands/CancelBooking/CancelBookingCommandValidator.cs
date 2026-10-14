using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Commands.CancelBooking;

public class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(command => command.BookingId)
            .NotEmpty().WithMessage("Booking ID is required.");
        RuleFor(command => command.Reason)
            .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}
