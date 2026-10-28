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

namespace PitchReserve.Application.Features.Bookings.Commands.RespondToJoinRequest;

public class RespondToJoinRequestCommandHandler : IRequestHandler<RespondToJoinRequestCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RespondToJoinRequestCommandHandler> _logger;

    public RespondToJoinRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        ILogger<RespondToJoinRequestCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(RespondToJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var organizerId = RequireCurrentPlayer();

        var booking = await _context.Bookings
            .Include(item => item.MatchParticipants)
            .FirstOrDefaultAsync(item => item.Id == request.BookingId, cancellationToken);
        if (booking is null)
            throw new NotFoundException(nameof(Booking), request.BookingId);

        if (booking.OrganizerPlayerId != organizerId)
            throw new ForbiddenAccessException("Only the match organizer can respond to join requests.");

        var now = _dateTimeProvider.UtcNow;
        EnsureMatchIsEligible(booking, now);

        var participant = booking.MatchParticipants
            .FirstOrDefault(item => item.Id == request.ParticipantId);
        if (participant is null)
            throw new NotFoundException(nameof(MatchParticipant), request.ParticipantId);

        if (request.Accept)
        {
            if (participant.Status == ParticipantStatus.Pending)
            {
                booking.AddAcceptedParticipant(now);
                participant.Accept();
                await _context.SaveChangesAsync(cancellationToken);
            }
            else if (participant.Status != ParticipantStatus.Joined)
            {
                throw new ConflictException("Only a pending request can be accepted.");
            }
        }
        else if (participant.Status == ParticipantStatus.Pending)
        {
            participant.Decline();
            await _context.SaveChangesAsync(cancellationToken);
        }
        else if (participant.Status != ParticipantStatus.Declined)
        {
            throw new ConflictException("Only a pending request can be declined.");
        }

        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger);
        return Result.Success();
    }

    private Guid RequireCurrentPlayer()
    {
        if (!_currentUserService.UserId.HasValue || !_currentUserService.IsAuthenticated)
            throw new ForbiddenAccessException("Authentication is required to respond to join requests.");
        if (!string.Equals(_currentUserService.Role, nameof(UserRole.Player), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only player accounts can decide match requests.");

        return _currentUserService.UserId.Value;
    }

    private static void EnsureMatchIsEligible(Booking booking, DateTime now)
    {
        if (!booking.IsOpenForPublicPlayers || !booking.MaxPlayersNeeded.HasValue)
            throw new ConflictException("This booking is private.");
        if (booking.Status == BookingStatus.Cancelled)
            throw new ConflictException("This match is no longer accepting participant decisions.");
        if (booking.Status != BookingStatus.AcceptedAwaitingCash && booking.Status != BookingStatus.Confirmed)
            throw new ConflictException("The field owner must accept this booking before participant requests can be decided.");
        if (booking.StartTime <= now || booking.ExpiresAt.HasValue && booking.ExpiresAt.Value <= now)
            throw new ConflictException("This match is no longer accepting participant decisions.");
    }
}
