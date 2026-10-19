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

namespace PitchReserve.Application.Features.Bookings.Commands.RejoinOrganizer;

public class RejoinOrganizerCommandHandler : IRequestHandler<RejoinOrganizerCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RejoinOrganizerCommandHandler> _logger;

    public RejoinOrganizerCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        ILogger<RejoinOrganizerCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(RejoinOrganizerCommand request, CancellationToken cancellationToken)
    {
        var organizerId = RequireCurrentPlayer();

        var booking = await _context.Bookings
            .Include(item => item.MatchParticipants)
            .FirstOrDefaultAsync(item => item.Id == request.BookingId, cancellationToken);
        if (booking is null)
            throw new NotFoundException(nameof(Booking), request.BookingId);
        if (booking.OrganizerPlayerId != organizerId)
            throw new ForbiddenAccessException("Only the original organizer can rejoin this match.");

        var now = _dateTimeProvider.UtcNow;
        if (booking.Status == BookingStatus.Cancelled)
            throw new ConflictException("This booking is no longer accepting organizer changes.");
        if (booking.Status != BookingStatus.AcceptedAwaitingCash && booking.Status != BookingStatus.Confirmed)
            throw new ConflictException("The field owner must accept this booking before the organizer can rejoin.");
        if (booking.StartTime <= now || booking.ExpiresAt.HasValue && booking.ExpiresAt.Value <= now)
            throw new ConflictException("This booking is no longer accepting organizer changes.");

        var participant = booking.MatchParticipants
            .FirstOrDefault(item => item.PlayerId == organizerId);
        if (participant is null)
            throw new ConflictException("The organizer's participant record is missing.");
        if (participant.Status == ParticipantStatus.Joined)
            return Result.Success();
        if (participant.Status != ParticipantStatus.Withdrawn)
            throw new ConflictException("Only a withdrawn organizer can rejoin this match.");

        booking.AddAcceptedParticipant(now, isOrganizerRejoin: true);
        participant.RejoinAsOrganizer(now);
        await _context.SaveChangesAsync(cancellationToken);

        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger);
        return Result.Success();
    }

    private Guid RequireCurrentPlayer()
    {
        if (!_currentUserService.UserId.HasValue || !_currentUserService.IsAuthenticated)
            throw new ForbiddenAccessException("Authentication is required to rejoin a match.");
        if (!string.Equals(_currentUserService.Role, nameof(UserRole.Player), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only player accounts can rejoin as an organizer.");

        return _currentUserService.UserId.Value;
    }
}
