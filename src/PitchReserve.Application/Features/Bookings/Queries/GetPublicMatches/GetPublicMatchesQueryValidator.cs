using FluentValidation;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetPublicMatches;

public class GetPublicMatchesQueryValidator : AbstractValidator<GetPublicMatchesQuery>
{
    public GetPublicMatchesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.PlaygroundId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Playground ID cannot be empty.");
        RuleFor(query => query.Status)
            .Must(status => !status.HasValue || Enum.IsDefined(status.Value))
            .WithMessage("Status is invalid.");
        RuleFor(query => query)
            .Must(query => !query.From.HasValue || !query.To.HasValue || query.From <= query.To)
            .WithMessage("From must be earlier than or equal to To.");
    }
}
