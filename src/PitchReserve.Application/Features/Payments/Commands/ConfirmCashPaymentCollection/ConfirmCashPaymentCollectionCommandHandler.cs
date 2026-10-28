using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Bookings.Common;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Payments.Commands.ConfirmCashPaymentCollection;

public class ConfirmCashPaymentCollectionCommandHandler : IRequestHandler<ConfirmCashPaymentCollectionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ConfirmCashPaymentCollectionCommandHandler> _logger;

    public ConfirmCashPaymentCollectionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        ILogger<ConfirmCashPaymentCollectionCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmCashPaymentCollectionCommand request, CancellationToken cancellationToken)
    {
        var bookingId = await _context.Bookings.AsNoTracking()
            .Where(b => b.Id == request.PaymentId || b.Payment.Id == request.PaymentId)
            .Select(b => (Guid?)b.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.PaymentId);

        var booking = await _context.Bookings.Include(b => b.Payment)
            .Include(b => b.Playground).ThenInclude(p => p.OwnerProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.PaymentId);

        if (_currentUserService.Role != "Owner" || !_currentUserService.UserId.HasValue ||
            booking.Playground.OwnerProfile.UserId != _currentUserService.UserId)
            throw new ForbiddenAccessException("Only the playground owner can confirm cash payment collection.");
        if (booking.Payment is null || booking.Payment.Method != PaymentMethod.CashOnArrival)
            throw new ConflictException("This booking does not have a cash payment.");
        if (booking.Status is not (BookingStatus.AcceptedAwaitingCash or BookingStatus.Confirmed))
            throw new ConflictException("Accept the booking request before recording cash collection.");

        booking.Payment.MarkAsPaid(request.PaidAt ?? _dateTimeProvider.UtcNow);
        booking.Confirm();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cash collected and booking confirmed for Booking {BookingId}, Payment {PaymentId}, Playground {PlaygroundId}",
            booking.Id,
            booking.Payment.Id,
            booking.PlaygroundId);

        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger, booking.PlaygroundId);
        try
        {
            await _cacheService.RemoveByPrefixAsync("payments", CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Payment cache invalidation failed with {ExceptionType}.", exception.GetType().Name);
        }

        return Result.Success();
    }
}
