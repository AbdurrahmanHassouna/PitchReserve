using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetOwnerBookings;

public class GetOwnerBookingsQueryHandler : IRequestHandler<GetOwnerBookingsQuery, List<OwnerBookingDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public GetOwnerBookingsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<List<OwnerBookingDto>> Handle(GetOwnerBookingsQuery request, CancellationToken cancellationToken)
    {

        if (!_currentUserService.UserId.HasValue ||  _currentUserService.UserId.Value != request.OwnerUserId)
        {
            throw new ForbiddenAccessException("User must be authenticated to view owner bookings.");
        }

        var key = $"bookings:{_currentUserService.UserId}:{request.Status}:{request.PlaygroundId}";
        var cachedBookings = await _cacheService.GetAsync<List<OwnerBookingDto>>(key, cancellationToken);
        if (cachedBookings is not null)
        {
            return cachedBookings;
        }
        var query = _context.Bookings
            .Include(b => b.Payment)
            .Include(b => b.MatchParticipants)
            .Include(b => b.Playground)
            .AsNoTracking()
            .Where(b => b.Playground.OwnerProfile.UserId == _currentUserService.UserId);

        if (request.Status.HasValue)
        {
            query = query.Where(b => b.Status == request.Status.Value);
        }

        if (request.PlaygroundId.HasValue)
        {
            query = query.Where(b => b.PlaygroundId == request.PlaygroundId.Value);
        }

        var bookings = await query
            .OrderByDescending(b => b.StartTime)
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
                PaymentMethod =  b.Payment.Method,
                PaymentAmount =  b.Payment.Amount ,
                TotalParticipants = b.MatchParticipants.Count,
                JoinedParticipants = b.MatchParticipants.Count(m => m.Status ==ParticipantStatus.Joined),
                CreatedAt = b.CreatedAt,
                BookingTime = b.BookingTime
            })
            .ToListAsync(cancellationToken);
        await _cacheService.SetAsync(key, bookings,TimeSpan.FromMinutes(15),cancellationToken);
        return bookings;
    }
}
