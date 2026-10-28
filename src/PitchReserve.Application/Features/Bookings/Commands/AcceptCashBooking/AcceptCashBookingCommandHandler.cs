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

namespace PitchReserve.Application.Features.Bookings.Commands.AcceptCashBooking;

public sealed class AcceptCashBookingCommandHandler : IRequestHandler<AcceptCashBookingCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ICacheService _cache;
    private readonly ILogger<AcceptCashBookingCommandHandler> _logger;

    public AcceptCashBookingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser,
        IDateTimeProvider clock, ICacheService cache,
        ILogger<AcceptCashBookingCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(AcceptCashBookingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue || _currentUser.Role != "Owner")
            throw new ForbiddenAccessException("Only the field owner can accept a cash booking request.");

        var booking = await _context.Bookings.Include(item => item.Payment)
            .Include(item => item.Playground).ThenInclude(field => field.OwnerProfile)
            .FirstOrDefaultAsync(item => item.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (booking.Playground.OwnerProfile.UserId != _currentUser.UserId.Value)
            throw new ForbiddenAccessException("This booking belongs to another field owner.");
        if (booking.Payment.Method != PaymentMethod.CashOnArrival)
            throw new ConflictException("Only cash bookings use owner request acceptance.");

        // Accept the request only. Cash collection later confirms the booking.
        booking.AcceptByOwner(_clock.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Field owner accepted cash booking {BookingId}.", request.BookingId);
        await BookingCacheInvalidator.InvalidateAsync(_cache, _logger, booking.PlaygroundId);
        return Result.Success();
    }
}
