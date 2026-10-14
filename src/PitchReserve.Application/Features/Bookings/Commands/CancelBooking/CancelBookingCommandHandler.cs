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

namespace PitchReserve.Application.Features.Bookings.Commands.CancelBooking;

public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CancelBookingCommandHandler> _logger;

    public CancelBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        ILogger<CancelBookingCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = RequireBookingActor();

        var booking = await _context.Bookings
            .Include(item => item.Playground)
                .ThenInclude(field => field.OwnerProfile)
            .Include(item => item.Payment)
            .FirstOrDefaultAsync(item => item.Id == request.BookingId, cancellationToken);
        if (booking is null)
            throw new NotFoundException(nameof(Booking), request.BookingId);

        var isOrganizer = booking.OrganizerPlayerId == actorUserId;
        var isFieldOwner = booking.Playground.OwnerProfile.UserId == actorUserId;
        if (!isOrganizer && !isFieldOwner)
            throw new ForbiddenAccessException("Only the organizer or field owner can cancel this booking.");

        // Idempotency.
        // No Delete ,soft one.
        if (booking.Status == BookingStatus.Cancelled)
            return Result.Success();

        if (booking.Payment.Status == PaymentStatus.Succeeded)
            throw new ConflictException("A paid booking cannot be cancelled until refund handling is available.");
        if (isFieldOwner && !isOrganizer && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.Reason)] = ["A cancellation reason is required for field-owner cancellation."]
            });
        }

        booking.Cancel(actorUserId, _dateTimeProvider.UtcNow, request.Reason);
        await _context.SaveChangesAsync(cancellationToken);

        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger, booking.PlaygroundId);
        return Result.Success();
    }

    private Guid RequireBookingActor()
    {
        if (!_currentUserService.UserId.HasValue || !_currentUserService.IsAuthenticated)
            throw new ForbiddenAccessException("Authentication is required to cancel a booking.");

        var role = _currentUserService.Role;
        if (!string.Equals(role, nameof(UserRole.Player), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(role, nameof(UserRole.Owner), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only organizers and field owners can cancel bookings.");

        return _currentUserService.UserId.Value;
    }
}
