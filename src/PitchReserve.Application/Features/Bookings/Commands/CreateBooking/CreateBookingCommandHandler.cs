using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Validation;
using PitchReserve.Application.Features.Bookings.Common;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBookingScheduleValidator _scheduleValidator;
    private readonly ITransactionCoordinator _transactionCoordinator;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateBookingCommandHandler> _logger;

    public CreateBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IBookingScheduleValidator scheduleValidator,
        ITransactionCoordinator transactionCoordinator,
        ICacheService cacheService,
        ILogger<CreateBookingCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _scheduleValidator = scheduleValidator;
        _transactionCoordinator = transactionCoordinator;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var organizerId = RequireCurrentPlayer();
        if (request.PaymentMethod != PaymentMethod.CashOnArrival)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.PaymentMethod)] = ["Only cash on arrival is available for new bookings."]
            });
        }

        var bookingId = await _transactionCoordinator.ExecuteAsync(
            [$"field:{request.PlaygroundId:D}"],
            async token =>
            {
                var organizer = await _context.Users
                    .FirstOrDefaultAsync(user => user.Id == organizerId, token);
                if (organizer is null)
                    throw new NotFoundException(nameof(User), organizerId);
                if (organizer.Role != UserRole.Player)
                    throw new ForbiddenAccessException("Only player accounts can create player bookings.");
                EnsureCashContact(organizer.PhoneNumber);

                var playground = await _scheduleValidator.ValidateAsync(
                    request.PlaygroundId, request.StartTime, request.EndTime, null, token);
                var utcNow = _dateTimeProvider.UtcNow;
                var startUtc = request.StartTime.UtcDateTime;
                var endUtc = request.EndTime.UtcDateTime;
                var durationHours = (decimal)(endUtc - startUtc).TotalHours;
                var totalPrice = Math.Round(durationHours * playground.HourlyRate, 2);

                var booking = Booking.Create(
                    playground.Id,
                    organizerId,
                    startUtc,
                    endUtc,
                    request.IsOpenForPublicPlayers,
                    request.MaxPlayersNeeded,
                    expiresAt: null,
                    utcNow);
                var payment = Payment.Create(booking.Id, totalPrice, PaymentMethod.CashOnArrival);
                var participant = MatchParticipant.Create(
                    booking.Id, organizerId, ParticipantStatus.Joined, utcNow);

                _context.Bookings.Add(booking);
                _context.Payments.Add(payment);
                _context.MatchParticipants.Add(participant);
                await _context.SaveChangesAsync(token);
                return booking.Id;
            },
            cancellationToken);

        _logger.LogInformation(
            "Booking created with Id {BookingId} for field {PlaygroundId}, organizer {OrganizerId}, status {BookingStatus}.",
            bookingId,
            request.PlaygroundId,
            organizerId,
            BookingStatus.PendingOwnerApproval);
        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger, request.PlaygroundId);
        return bookingId;
    }

    private Guid RequireCurrentPlayer()
    {
        if (!_currentUserService.UserId.HasValue || !_currentUserService.IsAuthenticated)
            throw new ForbiddenAccessException("Authentication is required to create a booking.");
        if (!string.Equals(_currentUserService.Role, nameof(UserRole.Player), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only player accounts can create player bookings.");

        return _currentUserService.UserId.Value;
    }

    private static void EnsureCashContact(string? phoneNumber)
    {
        if (PhoneNumberRules.IsValid(phoneNumber))
            return;

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["OrganizerPhoneNumber"] = ["A valid phone number is required for a cash booking."]
        });
    }
}
