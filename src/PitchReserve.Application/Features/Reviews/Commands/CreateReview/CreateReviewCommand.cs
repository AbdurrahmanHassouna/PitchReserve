using MediatR;
using PitchReserve.Application.Features.Reviews.Queries.GetPlaygroundReviews;

namespace PitchReserve.Application.Features.Reviews.Commands.CreateReview;

public sealed record CreateReviewCommand(Guid PlaygroundId, int Rating, string? Comment) : IRequest<ReviewDto>;
