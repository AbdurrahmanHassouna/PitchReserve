using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Bookings.Commands.RespondToJoinRequest;

public class RespondToJoinRequestCommandHandler : IRequestHandler<RespondToJoinRequestCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RespondToJoinRequestCommandHandler> _logger;

    public RespondToJoinRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        ILogger<RespondToJoinRequestCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(RespondToJoinRequestCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to respond to join requests.");
        }

        var key = $"lock:booking:{request.BookingId}";
        var token = Guid.NewGuid().ToString("N");
        var acquired = await _cacheService.TryAcquireLockAsync(
            key, token, TimeSpan.FromSeconds(15), cancellationToken);

        if (!acquired)
        {
            throw new SynchronousAccessException("This booking is being updated. Try again.");
        }

        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Playground)
                    .ThenInclude(p => p.OwnerProfile)
                .Include(b => b.MatchParticipants)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking is null)
            {
                throw new NotFoundException(nameof(Booking), request.BookingId);
            }
            var isOrganizer = booking.OrganizerPlayerId == _currentUserService.UserId;
            if (!isOrganizer)
            {
                throw new ForbiddenAccessException("Only the match organizer or pitch owner can respond to join requests.");
            }

            var participant = booking.MatchParticipants
                .FirstOrDefault(m => m.Id == request.ParticipantId);

            if (participant is null)
            {
                throw new NotFoundException(nameof(MatchParticipant), request.ParticipantId);
            }

            if (request.Accept)
            {
                if(participant.Status == ParticipantStatus.Pending)
                    booking.IncrementMatchParticipant();
                participant.Accept();

            }
            else
            {
                participant.Decline();
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _cacheService.RemoveByPrefixAsync("bookings", CancellationToken.None);

            return Result.Success();
        }
        finally
        {
            try
            {
                var released = await _cacheService.ReleaseLockAsync(key, token, CancellationToken.None);
                if (!released)
                {
                    _logger.LogWarning("Redis lease for booking {BookingId} was no longer owned during release.",
                        request.BookingId);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError("Failed to release Redis lease for booking {BookingId} with {ExceptionType}: {StackTrace}",
                    request.BookingId, exception.GetType().FullName, exception.StackTrace);
            }
        }
    }
}
