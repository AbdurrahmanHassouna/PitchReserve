using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Bookings.Queries.GetJoinRequests;

public class GetJoinRequestsQueryHandler : IRequestHandler<GetJoinRequestsQuery, PagedResult<JoinRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetJoinRequestsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<JoinRequestDto>> Handle(
        GetJoinRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to view join requests.");
        }

        if (!string.Equals(_currentUserService.Role, UserRole.Player.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("Only the booking organizer can view join requests.");
        }

        var booking = await _context.Bookings
            .AsNoTracking()
            .Where(item => item.Id == request.BookingId)
            .Select(item => new { item.Id, item.OrganizerPlayerId })
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
        {
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }

        if (booking.OrganizerPlayerId != _currentUserService.UserId.Value)
        {
            throw new ForbiddenAccessException("Only the booking organizer can view join requests.");
        }

        var query = _context.MatchParticipants
            .AsNoTracking()
            .Where(participant => participant.BookingId == request.BookingId &&
                                  participant.PlayerId != booking.OrganizerPlayerId);

        if (request.Status.HasValue)
        {
            query = query.Where(participant => participant.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min(((long)request.Page - 1) * request.PageSize, int.MaxValue);
        var items = await query
            .OrderByDescending(participant => participant.JoinedAt)
            .ThenByDescending(participant => participant.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(participant => new JoinRequestDto
            {
                ParticipantId = participant.Id,
                PlayerId = participant.PlayerId,
                PlayerName = participant.Player.FullName,
                Status = participant.Status,
                RequestedAt = participant.JoinedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<JoinRequestDto>(items, request.Page, request.PageSize, totalCount);
    }
}
