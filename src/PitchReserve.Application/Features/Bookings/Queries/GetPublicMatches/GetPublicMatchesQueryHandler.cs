using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetPublicMatches;

public class GetPublicMatchesQueryHandler : IRequestHandler<GetPublicMatchesQuery, PagedResult<PublicMatchDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetPublicMatchesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResult<PublicMatchDto>> Handle(
        GetPublicMatchesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to browse public matches.");
        }

        var userId = _currentUserService.UserId.Value;
        var isPlayer = string.Equals(
            _currentUserService.Role,
            UserRole.Player.ToString(),
            StringComparison.OrdinalIgnoreCase);
        var now = _dateTimeProvider.UtcNow;

        var query = _context.Bookings
            .AsNoTracking()
            .Where(booking => booking.IsOpenForPublicPlayers &&
                              booking.MaxPlayersNeeded.HasValue &&
                              (booking.Status == BookingStatus.AcceptedAwaitingCash ||
                               booking.Status == BookingStatus.Confirmed) &&
                              booking.StartTime > now &&
                              (!booking.ExpiresAt.HasValue || booking.ExpiresAt > now) &&
                              booking.Playground.Status != PlaygroundStatus.Inactive);

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
            .OrderBy(booking => booking.StartTime)
            .ThenBy(booking => booking.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(booking => new PublicMatchDto
            {
                BookingId = booking.Id,
                PlaygroundId = booking.PlaygroundId,
                PlaygroundName = booking.Playground.Name,
                LocationCity = booking.Playground.LocationCity,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status,
                MaxPlayersNeeded = booking.MaxPlayersNeeded!.Value,
                JoinedParticipantsCount = booking.MatchParticipants.Count(participant =>
                    participant.Status == ParticipantStatus.Joined),
                AvailablePlaces = booking.MaxPlayersNeeded.Value - booking.MatchParticipants.Count(participant =>
                    participant.Status == ParticipantStatus.Joined),
                MyParticipantStatus = booking.MatchParticipants
                    .Where(participant => participant.PlayerId == userId)
                    .Select(participant => (ParticipantStatus?)participant.Status)
                    .FirstOrDefault(),
                CanRequestToJoin = (booking.Status == BookingStatus.AcceptedAwaitingCash ||
                                    booking.Status == BookingStatus.Confirmed) &&
                                   isPlayer &&
                                   booking.OrganizerPlayerId != userId &&
                                   !booking.MatchParticipants.Any(participant =>
                                       participant.PlayerId == userId &&
                                       (participant.Status == ParticipantStatus.Joined ||
                                        participant.Status == ParticipantStatus.Withdrawn)),
                CanAcceptParticipant = (booking.Status == BookingStatus.AcceptedAwaitingCash ||
                                        booking.Status == BookingStatus.Confirmed) &&
                                       booking.OrganizerPlayerId == userId &&
                                       booking.MaxPlayersNeeded.Value > booking.MatchParticipants.Count(participant =>
                                           participant.Status == ParticipantStatus.Joined)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicMatchDto>(items, request.Page, request.PageSize, totalCount);
    }
}
