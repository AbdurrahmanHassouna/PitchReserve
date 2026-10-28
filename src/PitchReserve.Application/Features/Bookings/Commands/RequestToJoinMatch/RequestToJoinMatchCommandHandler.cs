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

namespace PitchReserve.Application.Features.Bookings.Commands.RequestToJoinMatch;

public class RequestToJoinMatchCommandHandler : IRequestHandler<RequestToJoinMatchCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RequestToJoinMatchCommandHandler> _logger;

    public RequestToJoinMatchCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        ILogger<RequestToJoinMatchCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(RequestToJoinMatchCommand request, CancellationToken cancellationToken)
    {
        var playerId = RequireCurrentPlayer();

        var player = await _context.Users.FirstOrDefaultAsync(user => user.Id == playerId, cancellationToken);
        if (player is null)
            throw new NotFoundException(nameof(User), playerId);
        if (player.Role != UserRole.Player)
            throw new ForbiddenAccessException("Only player accounts can request to join a match.");

        var booking = await _context.Bookings
            .Include(item => item.MatchParticipants)
            .FirstOrDefaultAsync(item => item.Id == request.BookingId, cancellationToken);
        if (booking is null)
            throw new NotFoundException(nameof(Booking), request.BookingId);

        var now = _dateTimeProvider.UtcNow;
        EnsurePublicMatchIsEligible(booking, now);
        if (booking.OrganizerPlayerId == playerId)
            throw new ConflictException("The organizer cannot request to join their own match.");

        var participant = booking.MatchParticipants.FirstOrDefault(item => item.PlayerId == playerId);
        Guid participantId;
        if (participant is null)
        {
            participant = MatchParticipant.Create(
                booking.Id, playerId, ParticipantStatus.Pending, now);
            booking.MatchParticipants.Add(participant);
            _context.MatchParticipants.Add(participant);
            await _context.SaveChangesAsync(cancellationToken);
            participantId = participant.Id;
        }
        else if (participant.Status == ParticipantStatus.Pending)
        {
            participantId = participant.Id;
        }
        else if (participant.Status == ParticipantStatus.Declined)
        {
            participant.ResubmitDeclinedRequest(now);
            await _context.SaveChangesAsync(cancellationToken);
            participantId = participant.Id;
        }
        else
        {
            throw new ConflictException(participant.Status == ParticipantStatus.Withdrawn
                ? "A withdrawn participant cannot request to rejoin this match."
                : "An accepted participant cannot request to join again.");
        }

        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger);
        return Result<Guid>.Success(participantId);
    }

    private Guid RequireCurrentPlayer()
    {
        if (!_currentUserService.UserId.HasValue || !_currentUserService.IsAuthenticated)
            throw new ForbiddenAccessException("Authentication is required to request to join a match.");
        if (!string.Equals(_currentUserService.Role, nameof(UserRole.Player), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only player accounts can request to join a match.");

        return _currentUserService.UserId.Value;
    }

    private static void EnsurePublicMatchIsEligible(Booking booking, DateTime now)
    {
        if (!booking.IsOpenForPublicPlayers || !booking.MaxPlayersNeeded.HasValue)
            throw new ConflictException("This booking is private.");
        if (booking.Status == BookingStatus.Cancelled)
            throw new ConflictException("This match is no longer accepting participant requests.");
        if (booking.Status != BookingStatus.AcceptedAwaitingCash && booking.Status != BookingStatus.Confirmed)
            throw new ConflictException("The field owner must accept this booking before players can join.");
        if (booking.StartTime <= now || booking.ExpiresAt.HasValue && booking.ExpiresAt.Value <= now)
            throw new ConflictException("This match is no longer accepting participant requests.");
    }
}
