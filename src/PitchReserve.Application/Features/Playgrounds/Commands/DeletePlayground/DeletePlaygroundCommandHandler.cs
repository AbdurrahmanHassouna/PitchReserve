using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Playgrounds.Commands.DeletePlayground;

public sealed class DeletePlaygroundCommandHandler : IRequestHandler<DeletePlaygroundCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ICacheService _cache;
    private readonly ILogger<DeletePlaygroundCommandHandler> _logger;

    public DeletePlaygroundCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser,
        IDateTimeProvider clock, ICacheService cache,
        ILogger<DeletePlaygroundCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(DeletePlaygroundCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue || _currentUser.Role != "Owner")
            throw new ForbiddenAccessException("Only the owning field owner can delete this playground.");

        var playground = await _context.Playgrounds
            .FirstOrDefaultAsync(field => field.Id == request.Id && field.OwnerProfile.UserId == _currentUser.UserId.Value,
                cancellationToken);
        if (playground is null)
            throw new NotFoundException(nameof(Playground), request.Id);

        var now = _clock.UtcNow;
        var hasFutureActiveBooking = await _context.Bookings.AnyAsync(booking =>
            booking.PlaygroundId == playground.Id
            && booking.Status != BookingStatus.Cancelled
            && booking.EndTime > now
            && (!booking.ExpiresAt.HasValue || booking.ExpiresAt > now), cancellationToken);
        if (hasFutureActiveBooking)
            throw new ConflictException("This playground has future active bookings and cannot be deleted.");

        var hasHistory = await _context.Bookings.AnyAsync(booking => booking.PlaygroundId == playground.Id, cancellationToken)
            || await _context.Payments.AnyAsync(payment => payment.Booking.PlaygroundId == playground.Id, cancellationToken);

        if (hasHistory)
        {
            playground.UpdateDetails(playground.Name, playground.Size, playground.SurfaceType,
                playground.HourlyRate, playground.LocationCity, playground.Address, playground.OpenHour,
                playground.CloseHour, PlaygroundStatus.Inactive, playground.Latitude, playground.Longitude);
        }
        else
        {
            var reviews = await _context.Reviews
                .Where(review => review.PlaygroundId == playground.Id)
                .ToListAsync(cancellationToken);
            _context.Reviews.RemoveRange(reviews);
            _context.Playgrounds.Remove(playground);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await InvalidateFieldCachesAsync(request.Id);
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
