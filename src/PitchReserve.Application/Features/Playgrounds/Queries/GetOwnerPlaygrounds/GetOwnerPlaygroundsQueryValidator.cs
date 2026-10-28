using FluentValidation;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetOwnerPlaygrounds;

public sealed class GetOwnerPlaygroundsQueryValidator : AbstractValidator<GetOwnerPlaygroundsQuery>
{
    public GetOwnerPlaygroundsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
