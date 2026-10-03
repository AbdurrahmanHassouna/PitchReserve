using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateBookingCommandHandler> _logger;

    public CreateBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        ILogger<CreateBookingCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var organizerId = request.OrganizerPlayerId;
        if (!_currentUserService.UserId.HasValue || _currentUserService.UserId != organizerId)
        {
            throw new ForbiddenAccessException("User must be authenticated to create a booking.");
        }

        var key = $"lock:playgrounds:{request.PlaygroundId}";
        var token = Guid.NewGuid().ToString("N");
        var acquired = await _cacheService.TryAcquireLockAsync(key, token, TimeSpan.FromSeconds(15), cancellationToken);
        if (!acquired)
            throw new SynchronousAccessException("this Playground is being updated. try again.");
        try
        {
            var playground = await _context.Playgrounds
                .FirstOrDefaultAsync(p => p.Id == request.PlaygroundId, cancellationToken);

            if (playground is null)
            {
                throw new NotFoundException(nameof(Playground), request.PlaygroundId);
            }
            await _cacheService.ExtendLockAsync(key,token,TimeSpan.FromSeconds(15),cancellationToken);
            var hasCollision = await _context.Bookings
                .AnyAsync(b =>
                        b.PlaygroundId == request.PlaygroundId &&
                        b.Status != BookingStatus.Cancelled &&
                        request.StartTime < b.EndTime &&
                        request.EndTime > b.StartTime &&
                        (b.ExpiresAt == null || b.ExpiresAt > _dateTimeProvider.UtcNow),
                    cancellationToken);

            if (hasCollision)
            {
                throw new ConflictException(
                    $"The selected time slot ({request.StartTime:yyyy-MM-dd HH:mm} - {request.EndTime:HH:mm}) is already booked for this playground.");
            }

            var durationHours = (decimal)(request.EndTime - request.StartTime).TotalHours;
            var totalPrice = Math.Round(durationHours * playground.HourlyRate, 2);

            var utcNow = _dateTimeProvider.UtcNow;

            var expiresAt = request.PaymentMethod == PaymentMethod.StripeCard ? utcNow.AddMinutes(15) : (DateTime?)null;

            var booking = Booking.Create(
                request.PlaygroundId,
                organizerId,
                request.StartTime,
                request.EndTime,
                request.IsOpenForPublicPlayers,
                request.MaxPlayersNeeded,
                expiresAt);


            var payment = Payment.Create(booking.Id, totalPrice, request.PaymentMethod);
            var participant = MatchParticipant.Create(booking.Id, organizerId, ParticipantStatus.Joined);

            _context.Bookings.Add(booking);
            _context.Payments.Add(payment);
            _context.MatchParticipants.Add(participant);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Booking created with Id {BookingId} for Playground {PlaygroundId}, Organizer {OrganizerId}, Status {BookingStatus}",
                booking.Id,
                booking.PlaygroundId,
                booking.OrganizerPlayerId,
                booking.Status);

            await _cacheService.RemoveByPrefixAsync("bookings", CancellationToken.None);
            await _cacheService.RemoveByPrefixAsync($"playgrounds:{request.PlaygroundId}", CancellationToken.None);
            return booking.Id;
        }
        finally
        {
            try
            {
                var released = await _cacheService.ReleaseLockAsync(key, token, CancellationToken.None);
                if (!released)
                {
                    _logger.LogWarning("Redis lease for Playground {PlaygroundId} was no longer owned during release.",
                        request.PlaygroundId);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError("Failed to release Redis lease for Playground {PlaygroundId} with {ExceptionType}: {StackTrace}",
                    request.PlaygroundId,
                    exception.GetType().FullName,
                    exception.StackTrace);
            }
        }
    }
}