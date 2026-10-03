using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Bookings.Commands.RequestToJoinMatch;

public class RequestToJoinMatchCommandHandler : IRequestHandler<RequestToJoinMatchCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public RequestToJoinMatchCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<Result<Guid>> Handle(RequestToJoinMatchCommand request, CancellationToken cancellationToken)
    {

        if (!_currentUserService.UserId.HasValue ||  _currentUserService.UserId.Value != request.PlayerId)
        {
            throw new ForbiddenAccessException("User must be authenticated to join this match.");
        }

        var booking = await _context.Bookings
            .Include(b => b.MatchParticipants)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }
        var existingParticipant = booking.MatchParticipants
            .FirstOrDefault(m => m.PlayerId == request.PlayerId);

        if (existingParticipant is not null)
        {

            existingParticipant.Rejoin();
            await _context.SaveChangesAsync(cancellationToken);
            await _cacheService.RemoveByPrefixAsync("bookings", CancellationToken.None);
            return Result<Guid>.Success(existingParticipant.Id);
        }

        var participant = MatchParticipant.Create(booking.Id, request.PlayerId, ParticipantStatus.Pending);
        booking.MatchParticipants.Add(participant);

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveByPrefixAsync("bookings", CancellationToken.None);

        return Result<Guid>.Success(participant.Id);
    }
}
