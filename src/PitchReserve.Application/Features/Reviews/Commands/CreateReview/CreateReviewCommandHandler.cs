using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Features.Reviews.Queries.GetPlaygroundReviews;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateReviewCommandHandler> _logger;

    public CreateReviewCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser,
        IDateTimeProvider clock, ICacheService cache,
        ILogger<CreateReviewCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue || _currentUser.Role != "Player")
            throw new ForbiddenAccessException("Only a player can create a review.");

        var playerId = _currentUser.UserId.Value;

        var playground = await _context.Playgrounds
            .FirstOrDefaultAsync(field => field.Id == request.PlaygroundId, cancellationToken);
        if (playground is null || playground.Status == PlaygroundStatus.Inactive)
            throw new NotFoundException(nameof(Playground), request.PlaygroundId);

        var duplicate = await _context.Reviews.AnyAsync(review =>
            review.PlaygroundId == request.PlaygroundId && review.PlayerId == playerId, cancellationToken);
        if (duplicate)
            throw new ConflictException("You have already reviewed this playground.");

        var now = _clock.UtcNow;
        var eligible = await _context.Bookings.AsNoTracking().AnyAsync(booking =>
            booking.PlaygroundId == request.PlaygroundId
            && (booking.Status == BookingStatus.AcceptedAwaitingCash
                || booking.Status == BookingStatus.Confirmed)
            && booking.EndTime <= now
            && (!booking.ExpiresAt.HasValue || booking.ExpiresAt.Value > booking.EndTime)
            && (booking.OrganizerPlayerId == playerId
                || booking.MatchParticipants.Any(participant => participant.PlayerId == playerId
                    && participant.Status == ParticipantStatus.Joined)), cancellationToken);
        if (!eligible)
            throw new ForbiddenAccessException("A review is available after completing a booking or accepted match on this playground.");

        var playerName = await _context.Users.AsNoTracking()
            .Where(user => user.Id == playerId)
            .Select(user => user.FullName)
            .FirstOrDefaultAsync(cancellationToken);
        if (playerName is null)
            throw new NotFoundException(nameof(User), playerId);

        var review = Review.Create(request.PlaygroundId, playerId, request.Rating, request.Comment);
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);
        await RefreshRatingAggregateAsync(playground, request.PlaygroundId, cancellationToken);

        var reviewDto = new ReviewDto
        {
            Id = review.Id,
            AuthorDisplayName = playerName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };

        await InvalidateFieldCachesAsync(request.PlaygroundId);
        return reviewDto;
    }

    private async Task RefreshRatingAggregateAsync(Playground playground, Guid playgroundId, CancellationToken token)
    {
        var query = _context.Reviews.Where(review => review.PlaygroundId == playgroundId);
        var count = await query.CountAsync(token);
        var average = count == 0 ? 0m : (await query.AverageAsync(review => (decimal)review.Rating, token));
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
