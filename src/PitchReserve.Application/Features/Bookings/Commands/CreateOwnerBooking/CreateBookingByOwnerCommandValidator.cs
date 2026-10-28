using FluentValidation;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Commands.CreateBookingByOwner;

public class CreateBookingByOwnerCommandValidator : AbstractValidator<CreateBookingByOwnerCommand>
{
    public CreateBookingByOwnerCommandValidator()
    {
        RuleFor(command => command.PlaygroundId)
            .NotEmpty().WithMessage("Field ID is required.");
        RuleFor(command => command.OrganizerPlayerId)
            .NotEmpty().WithMessage("Organizer player ID is required.");
        RuleFor(command => command.StartTime)
            .NotEmpty().WithMessage("Start time is required.");
        RuleFor(command => command.EndTime)
            .NotEmpty().WithMessage("End time is required.")
            .GreaterThan(command => command.StartTime)
            .WithMessage("End time must be after start time.");
        RuleFor(command => command.PaymentMethod)
            .Equal(PaymentMethod.CashOnArrival)
            .WithMessage("Only cash on arrival is available for new bookings.");
        RuleFor(command => command.MaxPlayersNeeded)
            .GreaterThan(0).When(command => command.IsOpenForPublicPlayers)
            .WithMessage("Total match capacity must include at least the organizer.");
    }
}
