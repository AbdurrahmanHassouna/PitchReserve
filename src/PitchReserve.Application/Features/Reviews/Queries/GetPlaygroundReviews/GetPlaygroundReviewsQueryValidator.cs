using FluentValidation;

namespace PitchReserve.Application.Features.Reviews.Queries.GetPlaygroundReviews;

public sealed class GetPlaygroundReviewsQueryValidator : AbstractValidator<GetPlaygroundReviewsQuery>
{
    public GetPlaygroundReviewsQueryValidator()
    {
        RuleFor(query => query.PlaygroundId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
