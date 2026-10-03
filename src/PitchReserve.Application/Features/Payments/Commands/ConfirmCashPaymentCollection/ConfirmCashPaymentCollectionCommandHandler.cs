using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
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
        var booking = await _context.Bookings
            .Include(b => b.Payment)
            .Include(b => b.Playground)
                .ThenInclude(p => p.OwnerProfile)
            .FirstOrDefaultAsync(b => b.Id == request.PaymentId ||  b.Payment.Id == request.PaymentId, cancellationToken);

        if (booking is null)
        {
            throw new NotFoundException(nameof(Booking), request.PaymentId);
        }

        if ( !_currentUserService.UserId.HasValue || booking.Playground.OwnerProfile.UserId != _currentUserService.UserId)
        {
            throw new ForbiddenAccessException("Only the playground owner can confirm cash payment collection.");
        }

        var paidAt = request.PaidAt??DateTime.UtcNow;

        booking.Payment.MarkAsPaid(paidAt);
        booking.Confirm();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cash payment confirmed and booking confirmed for Booking {BookingId}, Payment {PaymentId}, Playground {PlaygroundId}",
            booking.Id,
            booking.Payment.Id,
            booking.PlaygroundId);

        await _cacheService.RemoveAsync($"booking:{booking.Id}", CancellationToken.None);
        await _cacheService.RemoveByPrefixAsync("bookings", CancellationToken.None);
        await _cacheService.RemoveByPrefixAsync("payments", CancellationToken.None);
        if (booking.PlaygroundId != Guid.Empty)
        {
            await _cacheService.RemoveByPrefixAsync($"playgrounds:{booking.PlaygroundId}", CancellationToken.None);
        }

        return Result.Success();
    }
}
