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

namespace PitchReserve.Application.Features.Bookings.Commands.CreateBookingByOwner;

public class CreateBookingByOwnerCommandHandler : IRequestHandler<CreateBookingByOwnerCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBookingScheduleValidator _scheduleValidator;
    private readonly ITransactionCoordinator _transactionCoordinator;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateBookingByOwnerCommandHandler> _logger;

    public CreateBookingByOwnerCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IBookingScheduleValidator scheduleValidator,
        ITransactionCoordinator transactionCoordinator,
        ICacheService cacheService,
        ILogger<CreateBookingByOwnerCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _scheduleValidator = scheduleValidator;
        _transactionCoordinator = transactionCoordinator;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateBookingByOwnerCommand request, CancellationToken cancellationToken)
    {
        var ownerUserId = RequireCurrentOwner();
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
                var owner = await _context.Users
                    .FirstOrDefaultAsync(user => user.Id == ownerUserId, token);
                if (owner is null)
                    throw new NotFoundException(nameof(User), ownerUserId);
                if (owner.Role != UserRole.Owner)
                    throw new ForbiddenAccessException("Only owner accounts can create owner bookings.");

                var ownsField = await _context.Playgrounds.AnyAsync(
                    field => field.Id == request.PlaygroundId &&
                             field.OwnerProfile.UserId == ownerUserId,
                    token);
                if (!ownsField)
                {
                    var exists = await _context.Playgrounds.AnyAsync(
                        field => field.Id == request.PlaygroundId, token);
                    if (!exists)
                        throw new NotFoundException(nameof(Playground), request.PlaygroundId);
                    throw new ForbiddenAccessException("This field does not belong to the current owner.");
                }

                var organizer = await _context.Users
                    .FirstOrDefaultAsync(user => user.Id == request.OrganizerPlayerId, token);
                if (organizer is null)
                    throw new NotFoundException(nameof(User), request.OrganizerPlayerId);
                if (organizer.Role != UserRole.Player)
                    throw new ConflictException("The selected organizer must have a player account.");
                if (!PhoneNumberRules.IsValid(organizer.PhoneNumber))
                {
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        ["OrganizerPhoneNumber"] = ["The selected organizer must have a valid phone number for a cash booking."]
                    });
                }

                var playground = await _scheduleValidator.ValidateAsync(
                    request.PlaygroundId, request.StartTime, request.EndTime, null, token);
                var now = _dateTimeProvider.UtcNow;
                var startUtc = request.StartTime.UtcDateTime;
                var endUtc = request.EndTime.UtcDateTime;
                var durationHours = (decimal)(endUtc - startUtc).TotalHours;
                var totalPrice = Math.Round(durationHours * playground.HourlyRate, 2);

                var booking = Booking.Create(
                    playground.Id,
                    organizer.Id,
                    startUtc,
                    endUtc,
                    request.IsOpenForPublicPlayers,
                    request.MaxPlayersNeeded,
                    expiresAt: null,
                    now);
                var payment = Payment.Create(booking.Id, totalPrice, PaymentMethod.CashOnArrival);
                var organizerParticipant = MatchParticipant.Create(
                    booking.Id, organizer.Id, ParticipantStatus.Joined, now);

                _context.Bookings.Add(booking);
                _context.Payments.Add(payment);
                _context.MatchParticipants.Add(organizerParticipant);
                await _context.SaveChangesAsync(token);
                return booking.Id;
            },
            cancellationToken);

        _logger.LogInformation(
            "Owner booking created with Id {BookingId} for field {PlaygroundId}, organizer {OrganizerId}, status {BookingStatus}.",
            bookingId,
            request.PlaygroundId,
            request.OrganizerPlayerId,
            BookingStatus.PendingOwnerApproval);
        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger, request.PlaygroundId);
        return bookingId;
    }

    private Guid RequireCurrentOwner()
    {
        if (!_currentUserService.UserId.HasValue || !_currentUserService.IsAuthenticated)
            throw new ForbiddenAccessException("Authentication is required to create an owner booking.");
        if (!string.Equals(_currentUserService.Role, nameof(UserRole.Owner), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only owner accounts can create owner bookings.");

        return _currentUserService.UserId.Value;
    }
}
