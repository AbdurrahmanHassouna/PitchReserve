using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetMyBookings;

public class GetMyBookingsQueryHandler : IRequestHandler<GetMyBookingsQuery, PagedResult<MyBookingDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyBookingsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<MyBookingDto>> Handle(GetMyBookingsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to view bookings.");
        }

        if (!string.Equals(_currentUserService.Role, UserRole.Player.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("Only players can view their bookings.");
        }

        var userId = _currentUserService.UserId.Value;
        var query = _context.Bookings
            .AsNoTracking()
            .Where(booking => booking.OrganizerPlayerId == userId ||
                              (booking.Status == BookingStatus.AcceptedAwaitingCash ||
                               booking.Status == BookingStatus.Confirmed ||
                               booking.Status == BookingStatus.Cancelled) &&
                              booking.MatchParticipants.Any(participant =>
                                  participant.PlayerId == userId && participant.Status == ParticipantStatus.Joined));

        if (request.PlaygroundId.HasValue)
        {
            query = query.Where(booking => booking.PlaygroundId == request.PlaygroundId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(booking => booking.Status == request.Status.Value);
        }

        if (request.From.HasValue)
        {
            var from = request.From.Value.UtcDateTime;
            query = query.Where(booking => booking.StartTime >= from);
        }

        if (request.To.HasValue)
        {
            var to = request.To.Value.UtcDateTime;
            query = query.Where(booking => booking.StartTime <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min(((long)request.Page - 1) * request.PageSize, int.MaxValue);
        var items = await query
            .OrderByDescending(booking => booking.StartTime)
            .ThenByDescending(booking => booking.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(booking => new MyBookingDto
            {
                BookingId = booking.Id,
                PlaygroundId = booking.PlaygroundId,
                PlaygroundName = booking.Playground.Name,
                OrganizerPlayerId = booking.OrganizerPlayerId,
                OrganizerPlayerName = booking.OrganizerPlayer.FullName,
                OrganizerPhoneNumber = booking.OrganizerPlayerId == userId
                    ? booking.OrganizerPlayer.PhoneNumber
                    : null,
                BookingTime = booking.BookingTime,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status,
                IsOpenForPublicPlayers = booking.IsOpenForPublicPlayers,
                MaxPlayersNeeded = booking.MaxPlayersNeeded,
                JoinedParticipantsCount = booking.MatchParticipants.Count(participant =>
                    participant.Status == ParticipantStatus.Joined),
                AvailablePlaces = booking.MaxPlayersNeeded.HasValue
                    ? booking.MaxPlayersNeeded.Value - booking.MatchParticipants.Count(participant =>
                        participant.Status == ParticipantStatus.Joined)
                    : null,
                MyParticipantStatus = booking.MatchParticipants
                    .Where(participant => participant.PlayerId == userId)
                    .Select(participant => (ParticipantStatus?)participant.Status)
                    .FirstOrDefault(),
                PaymentStatus = booking.OrganizerPlayerId == userId ? booking.Payment.Status : null,
                PaymentMethod = booking.OrganizerPlayerId == userId ? booking.Payment.Method : null,
                PaymentAmount = booking.OrganizerPlayerId == userId ? booking.Payment.Amount : null,
                CreatedAt = booking.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<MyBookingDto>(items, request.Page, request.PageSize, totalCount);
    }
}
