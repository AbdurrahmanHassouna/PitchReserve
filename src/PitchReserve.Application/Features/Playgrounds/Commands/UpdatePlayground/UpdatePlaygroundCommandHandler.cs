using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Playgrounds.Commands.UpdatePlayground;

public sealed class UpdatePlaygroundCommandHandler : IRequestHandler<UpdatePlaygroundCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBookingScheduleValidator _scheduleValidator;
    private readonly IDateTimeProvider _clock;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdatePlaygroundCommandHandler> _logger;

    public UpdatePlaygroundCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser,
        IBookingScheduleValidator scheduleValidator, IDateTimeProvider clock, ICacheService cache,
        ILogger<UpdatePlaygroundCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _scheduleValidator = scheduleValidator;
        _clock = clock;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(UpdatePlaygroundCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue || _currentUser.Role != "Owner")
            throw new ForbiddenAccessException("Only the owning field owner can update this playground.");

        var playground = await _context.Playgrounds
            .FirstOrDefaultAsync(field => field.Id == request.Id && field.OwnerProfile.UserId == _currentUser.UserId.Value,
                cancellationToken);
        if (playground is null)
            throw new NotFoundException(nameof(Playground), request.Id);

        var hoursChanged = playground.OpenHour != request.OpenHour || playground.CloseHour != request.CloseHour;
        if (hoursChanged)
        {
            var now = _clock.UtcNow;
            var futureBookings = await _context.Bookings.AsNoTracking()
                .Where(booking => booking.PlaygroundId == playground.Id
                    && booking.Status != BookingStatus.Cancelled
                    && booking.EndTime > now
                    && (!booking.ExpiresAt.HasValue || booking.ExpiresAt > now))
                .OrderBy(booking => booking.StartTime)
                .ToListAsync(cancellationToken);

            foreach (var booking in futureBookings)
            {
                if (!_scheduleValidator.ValidateOpeningWindow(
                        AsUtcOffset(booking.StartTime), AsUtcOffset(booking.EndTime), request.OpenHour, request.CloseHour))
                    throw new ConflictException("Opening hours cannot change because an active booking would fall outside the new hours.");
            }
        }

        playground.UpdateDetails(request.Name, request.Size, request.SurfaceType, request.HourlyRate,
            request.LocationCity, request.Address, request.OpenHour, request.CloseHour, request.Status,
            request.Latitude, request.Longitude);
        await _context.SaveChangesAsync(cancellationToken);

        await InvalidateFieldCachesAsync(request.Id);
    }

    private static DateTimeOffset AsUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

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
