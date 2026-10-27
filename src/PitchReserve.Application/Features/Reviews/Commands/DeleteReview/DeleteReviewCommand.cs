using MediatR;

namespace PitchReserve.Application.Features.Reviews.Commands.DeleteReview;

public sealed record DeleteReviewCommand(Guid Id) : IRequest;
