using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Commands.AcceptCashBooking;

public sealed class AcceptCashBookingCommandValidator : AbstractValidator<AcceptCashBookingCommand>
{
    public AcceptCashBookingCommandValidator()
    {
        RuleFor(command => command.BookingId).NotEmpty();
    }
}
