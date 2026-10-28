using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Reviews.Queries.GetPlaygroundReviews;

public sealed record GetPlaygroundReviewsQuery(Guid PlaygroundId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ReviewDto>>;
