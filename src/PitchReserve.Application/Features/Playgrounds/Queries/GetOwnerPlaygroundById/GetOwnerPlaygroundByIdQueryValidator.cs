using FluentValidation;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetOwnerPlaygroundById;

public sealed class GetOwnerPlaygroundByIdQueryValidator : AbstractValidator<GetOwnerPlaygroundByIdQuery>
{
    public GetOwnerPlaygroundByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
