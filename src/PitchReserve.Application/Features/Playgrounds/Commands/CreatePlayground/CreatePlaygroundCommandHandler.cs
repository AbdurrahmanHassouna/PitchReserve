using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Playgrounds.Commands.CreatePlayground;

public class CreatePlaygroundCommandHandler : IRequestHandler<CreatePlaygroundCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreatePlaygroundCommandHandler> _logger;

    public CreatePlaygroundCommandHandler(
        IApplicationDbContext context,
        ICacheService cacheService,
        ICurrentUserService currentUserService,
        ILogger<CreatePlaygroundCommandHandler> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreatePlaygroundCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue || _currentUserService.Role != "Owner")
            throw new ForbiddenAccessException("An Owner must be authenticated to create a playground.");
        var ownerProfile = await _context.OwnerProfiles
            .FirstOrDefaultAsync(o => o.UserId == _currentUserService.UserId.Value, cancellationToken);
        if (ownerProfile is null)
        {
            throw new ForbiddenAccessException("An Owner must be authenticated to create a playground.");
        }

        var playground = Playground.Create(
            ownerProfile.Id,
            request.Name,
            request.Size,
            request.SurfaceType,
            request.HourlyRate,
            request.LocationCity,
            request.Address,
            request.OpenHour,
            request.CloseHour,
            request.Status,
            request.Latitude,
            request.Longitude);

        _context.Playgrounds.Add(playground);
        await _context.SaveChangesAsync(cancellationToken);

        await InvalidateFieldCachesAsync(playground.Id);

        return playground.Id;
    }

    private async Task InvalidateFieldCachesAsync(Guid playgroundId)
    {
        try
        {
            await _cacheService.RemoveByPrefixAsync("playgrounds", CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Playground list cache invalidation failed for {PlaygroundId} ({ExceptionType}).",
                playgroundId, exception.GetType().Name);
        }

        try
        {
            await _cacheService.RemoveAsync($"playground:{playgroundId}", CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Playground detail cache invalidation failed for {PlaygroundId} ({ExceptionType}).",
                playgroundId, exception.GetType().Name);
        }
    }
}
