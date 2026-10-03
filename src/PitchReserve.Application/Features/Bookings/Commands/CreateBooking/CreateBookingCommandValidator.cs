using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.PlaygroundId)
            .NotEmpty().WithMessage("Playground ID is required.");

        RuleFor(x => x.OrganizerPlayerId)
            .NotEmpty().WithMessage("Organizer player ID is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.")
            .Must(t=> t.Minute==0 && t.Second==0).WithMessage("Booking must be Hour based no fractional minutes.")
            .LessThan(x => x.EndTime).WithMessage("Start time must be earlier than end time.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("A valid payment Method must be specified.");

        RuleFor(x => x.MaxPlayersNeeded)
            .GreaterThan(0).When(x => x.IsOpenForPublicPlayers)
            .WithMessage("Max players needed must be greater than zero when open for public players.");
    }
}
