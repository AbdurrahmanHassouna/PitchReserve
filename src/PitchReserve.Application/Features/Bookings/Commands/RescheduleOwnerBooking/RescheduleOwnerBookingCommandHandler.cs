using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Bookings.Common;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Bookings.Commands.RescheduleOwnerBooking;

public class RescheduleOwnerBookingCommandHandler : IRequestHandler<RescheduleOwnerBookingCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBookingScheduleValidator _scheduleValidator;
    private readonly ITransactionCoordinator _transactionCoordinator;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RescheduleOwnerBookingCommandHandler> _logger;

    public RescheduleOwnerBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IBookingScheduleValidator scheduleValidator,
        ITransactionCoordinator transactionCoordinator,
        ICacheService cacheService,
        ILogger<RescheduleOwnerBookingCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _scheduleValidator = scheduleValidator;
        _transactionCoordinator = transactionCoordinator;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(RescheduleOwnerBookingCommand request, CancellationToken cancellationToken)
    {
        var ownerUserId = RequireCurrentOwner();
        var initialFieldId = await _context.Bookings
            .AsNoTracking()
            .Where(booking => booking.Id == request.BookingId)
            .Select(booking => (Guid?)booking.PlaygroundId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!initialFieldId.HasValue)
            throw new NotFoundException(nameof(Booking), request.BookingId);

        await _transactionCoordinator.ExecuteAsync(
            [$"booking:{request.BookingId:D}", $"field:{initialFieldId.Value:D}"],
            async token =>
            {
                var booking = await _context.Bookings
                    .Include(item => item.Playground)
                        .ThenInclude(field => field.OwnerProfile)
                    .FirstOrDefaultAsync(item => item.Id == request.BookingId, token);
                if (booking is null)
                    throw new NotFoundException(nameof(Booking), request.BookingId);
                if (booking.PlaygroundId != initialFieldId.Value)
                    throw new ConflictException("The booking's field changed while the reschedule was starting. Retry the request.");
                if (booking.Playground.OwnerProfile.UserId != ownerUserId)
                    throw new ForbiddenAccessException("This booking belongs to another field owner.");

                booking.Reschedule(
                    request.StartTime.UtcDateTime,
                    request.EndTime.UtcDateTime,
                    _dateTimeProvider.UtcNow);
                await _scheduleValidator.ValidateAsync(
                    booking.PlaygroundId,
                    request.StartTime,
                    request.EndTime,
                    booking.Id,
                    token);
                await _context.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);

        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger, initialFieldId.Value);
        return Result.Success();
    }

    private Guid RequireCurrentOwner()
    {
        if (!_currentUserService.UserId.HasValue || !_currentUserService.IsAuthenticated)
            throw new ForbiddenAccessException("Authentication is required to reschedule a booking.");
        if (!string.Equals(_currentUserService.Role, nameof(UserRole.Owner), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only field owners can reschedule bookings.");

        return _currentUserService.UserId.Value;
    }
}
