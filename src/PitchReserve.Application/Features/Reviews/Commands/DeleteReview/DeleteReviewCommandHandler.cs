using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;
    private readonly ILogger<DeleteReviewCommandHandler> _logger;

    public DeleteReviewCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser,
        ICacheService cache, ILogger<DeleteReviewCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue || _currentUser.Role != "Player")
            throw new ForbiddenAccessException("Only the review author can remove a review.");

        var review = await _context.Reviews
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (review is null)
            throw new NotFoundException(nameof(Review), request.Id);
        if (review.PlayerId != _currentUser.UserId.Value)
            throw new ForbiddenAccessException("Only the review author can remove a review.");

        var playground = await _context.Playgrounds
            .FirstOrDefaultAsync(field => field.Id == review.PlaygroundId, cancellationToken);
        if (playground is null)
            throw new NotFoundException(nameof(Playground), review.PlaygroundId);

        var playgroundId = review.PlaygroundId;
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
        await RefreshRatingAggregateAsync(playground, playgroundId, cancellationToken);

        await InvalidateFieldCachesAsync(playgroundId);
    }

    private async Task RefreshRatingAggregateAsync(Playground playground, Guid playgroundId, CancellationToken token)
    {
        var query = _context.Reviews.Where(review => review.PlaygroundId == playgroundId);
        var count = await query.CountAsync(token);
        var average = count == 0 ? 0m : await query.AverageAsync(review => (decimal)review.Rating, token);
        playground.SetRatingAggregate(count, average);
        await _context.SaveChangesAsync(token);
    }

    private async Task InvalidateFieldCachesAsync(Guid playgroundId)
    {
        try
        {
            await _cache.RemoveByPrefixAsync("playgrounds", CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Playground list cache invalidation failed for {PlaygroundId} ({ExceptionType}).",
                playgroundId, exception.GetType().Name);
        }

        try
        {
            await _cache.RemoveAsync($"playground:{playgroundId}", CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Playground detail cache invalidation failed for {PlaygroundId} ({ExceptionType}).",
                playgroundId, exception.GetType().Name);
        }
    }
}
