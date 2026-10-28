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

namespace PitchReserve.Application.Features.Bookings.Commands.WithdrawFromMatch;

public class WithdrawFromMatchCommandHandler : IRequestHandler<WithdrawFromMatchCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<WithdrawFromMatchCommandHandler> _logger;

    public WithdrawFromMatchCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        ILogger<WithdrawFromMatchCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(WithdrawFromMatchCommand request, CancellationToken cancellationToken)
    {
        var playerId = RequireCurrentPlayer();

        var booking = await _context.Bookings
            .Include(item => item.MatchParticipants)
            .FirstOrDefaultAsync(item => item.Id == request.BookingId, cancellationToken);
        if (booking is null)
            throw new NotFoundException(nameof(Booking), request.BookingId);

        var now = _dateTimeProvider.UtcNow;
        EnsureBookingIsUpcoming(booking, now);

        var participant = booking.MatchParticipants.FirstOrDefault(item => item.PlayerId == playerId);
        if (participant is null)
            throw new ConflictException("You are not an accepted participant in this match.");
        if (participant.Status == ParticipantStatus.Withdrawn)
            return Result.Success();
        if (participant.Status != ParticipantStatus.Joined)
            throw new ConflictException("Only an accepted participant can withdraw.");

        booking.RemoveAcceptedParticipant(now);
        participant.Withdraw();
        await _context.SaveChangesAsync(cancellationToken);

        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger);
        return Result.Success();
    }

    private Guid RequireCurrentPlayer()
    {
        if (!_currentUserService.UserId.HasValue || !_currentUserService.IsAuthenticated)
            throw new ForbiddenAccessException("Authentication is required to withdraw from a match.");
        if (!string.Equals(_currentUserService.Role, nameof(UserRole.Player), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only player accounts can withdraw from a match.");

        return _currentUserService.UserId.Value;
    }

    private static void EnsureBookingIsUpcoming(Booking booking, DateTime now)
    {
        if (booking.Status == BookingStatus.Cancelled)
            throw new ConflictException("This booking is no longer accepting participant changes.");
        if (booking.Status != BookingStatus.AcceptedAwaitingCash && booking.Status != BookingStatus.Confirmed)
            throw new ConflictException("The field owner must accept this booking before participants can withdraw.");
        if (booking.StartTime <= now || booking.ExpiresAt.HasValue && booking.ExpiresAt.Value <= now)
            throw new ConflictException("This booking is no longer accepting participant changes.");
    }
}
