using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Commands.UpdateBookingMatchSettings;

public class UpdateBookingMatchSettingsCommandValidator : AbstractValidator<UpdateBookingMatchSettingsCommand>
{
    public UpdateBookingMatchSettingsCommandValidator()
    {
        RuleFor(command => command.BookingId)
            .NotEmpty();

        RuleFor(command => command.IsOpenForPublicPlayers)
            .NotNull();

        RuleFor(command => command.MaxPlayersNeeded)
            .NotNull()
            .GreaterThan(0)
            .When(command => command.IsOpenForPublicPlayers == true);
    }
}
