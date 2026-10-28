using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Commands.RescheduleOwnerBooking;

public class RescheduleOwnerBookingCommandValidator : AbstractValidator<RescheduleOwnerBookingCommand>
{
    public RescheduleOwnerBookingCommandValidator()
    {
        RuleFor(command => command.BookingId)
            .NotEmpty().WithMessage("Booking ID is required.");
        RuleFor(command => command.StartTime)
            .NotEmpty().WithMessage("Start time is required.");
        RuleFor(command => command.EndTime)
            .NotEmpty().WithMessage("End time is required.")
            .GreaterThan(command => command.StartTime)
            .WithMessage("End time must be after start time.");
    }
}
