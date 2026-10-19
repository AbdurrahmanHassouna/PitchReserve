using FluentValidation;

namespace PitchReserve.Application.Features.Bookings.Commands.RespondToJoinRequest;

public class RespondToJoinRequestCommandValidator : AbstractValidator<RespondToJoinRequestCommand>
{
    public RespondToJoinRequestCommandValidator()
    {
        RuleFor(v => v.BookingId)
            .NotEmpty().WithMessage("Booking ID is required.");

        RuleFor(v => v.ParticipantId)
            .NotEmpty().WithMessage("Participant ID is required.");
    }
}
