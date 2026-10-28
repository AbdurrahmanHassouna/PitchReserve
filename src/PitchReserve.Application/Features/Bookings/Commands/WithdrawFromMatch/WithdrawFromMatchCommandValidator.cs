using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Commands.WithdrawFromMatch;

public class WithdrawFromMatchCommandValidator : AbstractValidator<WithdrawFromMatchCommand>
{
    public WithdrawFromMatchCommandValidator()
    {
        RuleFor(command => command.BookingId)
            .NotEmpty().WithMessage("Booking ID is required.");
    }
}
