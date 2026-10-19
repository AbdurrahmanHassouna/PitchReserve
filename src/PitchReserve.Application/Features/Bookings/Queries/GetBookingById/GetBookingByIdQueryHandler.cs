using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Bookings.Queries.GetBookingById;

public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDetailsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBookingByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BookingDetailsDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to view booking details.");
        }

        var userId = _currentUserService.UserId.Value;
        var isPlayer = string.Equals(
            _currentUserService.Role,
            UserRole.Player.ToString(),
            StringComparison.OrdinalIgnoreCase);
        var isOwner = string.Equals(
            _currentUserService.Role,
            UserRole.Owner.ToString(),
            StringComparison.OrdinalIgnoreCase);

        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(item => item.Playground)
                .ThenInclude(playground => playground.OwnerProfile)
            .Include(item => item.OrganizerPlayer)
            .Include(item => item.Payment)
            .Include(item => item.MatchParticipants.Where(participant =>
                participant.Status == ParticipantStatus.Joined || participant.PlayerId == userId))
                .ThenInclude(participant => participant.Player)
            .FirstOrDefaultAsync(item => item.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }

        var isOrganizer = booking.OrganizerPlayerId == userId;
        var isFieldOwner = isOwner && booking.Playground.OwnerProfile.UserId == userId;
        var isAcceptedParticipant = (booking.Status == BookingStatus.AcceptedAwaitingCash ||
                                     booking.Status == BookingStatus.Confirmed ||
                                     booking.Status == BookingStatus.Cancelled) &&
                                    isPlayer &&
                                    booking.MatchParticipants.Any(participant =>
                                        participant.PlayerId == userId && participant.Status == ParticipantStatus.Joined);
        var canViewDetails = isOrganizer || isFieldOwner || isAcceptedParticipant;
        var canViewBookingContactAndPayment = isOrganizer || isFieldOwner;
        var publicMatchCanBeViewed = booking.IsOpenForPublicPlayers &&
                                     booking.MaxPlayersNeeded.HasValue &&
                                     (booking.Status == BookingStatus.AcceptedAwaitingCash ||
                                      booking.Status == BookingStatus.Confirmed) &&
                                     booking.Playground.Status != PlaygroundStatus.Inactive;

        if (!canViewDetails && !publicMatchCanBeViewed)
        {
            // Hide private booking existence from unrelated callers.
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }

        var myParticipantStatus = booking.MatchParticipants
            .Where(participant => participant.PlayerId == userId)
            .Select(participant => (ParticipantStatus?)participant.Status)
            .FirstOrDefault();
        var joinedCount = booking.MatchParticipants.Count(participant =>
            participant.Status == ParticipantStatus.Joined);
        var availablePlaces = booking.MaxPlayersNeeded.HasValue
            ? Math.Max(booking.MaxPlayersNeeded.Value - joinedCount, 0)
            : (int?)null;

        return new BookingDetailsDto
        {
            BookingId = booking.Id,
            PlaygroundId = booking.PlaygroundId,
            PlaygroundName = booking.Playground.Name,
            LocationCity = booking.Playground.LocationCity,
            Address = booking.Playground.Address,
            PlaygroundStatus = booking.Playground.Status,
            OrganizerPlayerId = canViewDetails ? booking.OrganizerPlayerId : null,
            OrganizerPlayerName = canViewDetails ? booking.OrganizerPlayer.FullName : null,
            OrganizerPhoneNumber = canViewBookingContactAndPayment ? booking.OrganizerPlayer.PhoneNumber : null,
            BookingTime = booking.BookingTime,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            IsOpenForPublicPlayers = booking.IsOpenForPublicPlayers,
            MaxPlayersNeeded = booking.MaxPlayersNeeded,
            JoinedParticipantsCount = joinedCount,
            AvailablePlaces = availablePlaces,
            MyParticipantStatus = myParticipantStatus,
            PaymentStatus = canViewBookingContactAndPayment ? booking.Payment?.Status : null,
            PaymentMethod = canViewBookingContactAndPayment ? booking.Payment?.Method : null,
            PaymentAmount = canViewBookingContactAndPayment ? booking.Payment?.Amount : null,
            CancelledAt = canViewBookingContactAndPayment ? booking.CancelledAt : null,
            CancellationReason = canViewBookingContactAndPayment ? booking.CancellationReason : null,
            Participants = canViewDetails
                ? booking.MatchParticipants
                    .Where(participant => participant.Status == ParticipantStatus.Joined)
                    .OrderBy(participant => participant.JoinedAt)
                    .ThenBy(participant => participant.PlayerId)
                    .Select(participant => new BookingParticipantDto
                    {
                        PlayerId = participant.PlayerId,
                        PlayerName = participant.Player.FullName,
                        Status = participant.Status,
                        JoinedAt = participant.JoinedAt,
                        IsOrganizer = participant.PlayerId == booking.OrganizerPlayerId
                    })
                    .ToArray()
                : Array.Empty<BookingParticipantDto>()
        };
    }
}
