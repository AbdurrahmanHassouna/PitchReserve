using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Bookings.Queries.GetOwnerBookings;

public class GetOwnerBookingsQueryHandler : IRequestHandler<GetOwnerBookingsQuery, List<OwnerBookingDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOwnerBookingsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<OwnerBookingDto>> Handle(GetOwnerBookingsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to view owner bookings.");
        }

        if (!string.Equals(_currentUserService.Role, UserRole.Owner.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("Only field owners can view owner bookings.");
        }

        var ownerUserId = _currentUserService.UserId.Value;
        var query = _context.Bookings
            .AsNoTracking()
            .Where(b => b.Playground.OwnerProfile.UserId == ownerUserId);

        if (request.Status.HasValue)
        {
            query = query.Where(b => b.Status == request.Status.Value);
        }

        if (request.PlaygroundId.HasValue)
        {
            query = query.Where(b => b.PlaygroundId == request.PlaygroundId.Value);
        }

        return await query
            .OrderByDescending(b => b.StartTime)
            .ThenByDescending(b => b.Id)
            .Select(b => new OwnerBookingDto
            {
                BookingId = b.Id,
                PlaygroundId = b.PlaygroundId,
                PlaygroundName = b.Playground.Name,
                OrganizerPlayerId = b.OrganizerPlayerId,
                OrganizerPlayerName = b.OrganizerPlayer.FullName,
                OrganizerPhoneNumber = b.OrganizerPlayer.PhoneNumber,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status,
                PaymentStatus = b.Payment.Status,
                PaymentMethod = b.Payment.Method,
                PaymentAmount = b.Payment.Amount,
                TotalParticipants = b.MatchParticipants.Count,
                JoinedParticipants = b.MatchParticipants.Count(m => m.Status == ParticipantStatus.Joined),
                PendingParticipants = b.MatchParticipants.Count(m => m.Status == ParticipantStatus.Pending),
                CreatedAt = b.CreatedAt,
                BookingTime = b.BookingTime
            })
            .ToListAsync(cancellationToken);
    }
}
