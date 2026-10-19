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

namespace PitchReserve.Application.Features.Bookings.Commands.UpdateBookingMatchSettings;

public class UpdateBookingMatchSettingsCommandHandler : IRequestHandler<UpdateBookingMatchSettingsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UpdateBookingMatchSettingsCommandHandler> _logger;

    public UpdateBookingMatchSettingsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        ILogger<UpdateBookingMatchSettingsCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateBookingMatchSettingsCommand request, CancellationToken cancellationToken)
    {
        var playerId = RequireCurrentPlayer();

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(item => item.Id == request.BookingId, cancellationToken);
        if (booking is null)
            throw new NotFoundException(nameof(Booking), request.BookingId);
        if (booking.OrganizerPlayerId != playerId)
            throw new ForbiddenAccessException("Only the organizer can change match settings.");

        booking.UpdateMatchSettings(
            request.IsOpenForPublicPlayers!.Value,
            request.MaxPlayersNeeded,
            _dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        await BookingCacheInvalidator.InvalidateAsync(_cacheService, _logger);
        return Result.Success();
    }

    private Guid RequireCurrentPlayer()
    {
        if (!_currentUserService.UserId.HasValue || !_currentUserService.IsAuthenticated)
            throw new ForbiddenAccessException("Authentication is required to change match settings.");
        if (!string.Equals(_currentUserService.Role, nameof(UserRole.Player), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only player accounts can change match settings.");

        return _currentUserService.UserId.Value;
    }
}
