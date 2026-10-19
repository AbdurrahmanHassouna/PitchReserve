using FluentValidation;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetOwnerBookings;

public class GetOwnerBookingsQueryValidator : AbstractValidator<GetOwnerBookingsQuery>
{
    public GetOwnerBookingsQueryValidator()
    {
        RuleFor(query => query.PlaygroundId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Playground ID cannot be empty.");

        RuleFor(query => query.Status)
            .Must(status => !status.HasValue || Enum.IsDefined(status.Value))
            .WithMessage("Status is invalid.");
    }
}
