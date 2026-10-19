using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Commands.RejoinOrganizer;

public class RejoinOrganizerCommandValidator : AbstractValidator<RejoinOrganizerCommand>
{
    public RejoinOrganizerCommandValidator()
    {
        RuleFor(command => command.BookingId)
            .NotEmpty().WithMessage("Booking ID is required.");
    }
}
