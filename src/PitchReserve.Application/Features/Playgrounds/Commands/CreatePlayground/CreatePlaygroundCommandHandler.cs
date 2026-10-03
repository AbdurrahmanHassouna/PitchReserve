using MediatR;
using Microsoft.EntityFrameworkCore;
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

    public CreatePlaygroundCommandHandler(
        IApplicationDbContext context,
        ICacheService cacheService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _cacheService = cacheService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreatePlaygroundCommand request, CancellationToken cancellationToken)
    {
        if(!_currentUserService.UserId.HasValue)
            throw new ForbiddenAccessException("An Owner must be authenticated to create a playground.");
        var ownerProfile = await _context.OwnerProfiles
            .FirstOrDefaultAsync(o => o.UserId == _currentUserService.UserId, cancellationToken);
        if (ownerProfile is null||
            _currentUserService.UserId != ownerProfile.UserId)
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

        await _cacheService.RemoveByPrefixAsync("playgrounds", CancellationToken.None);
        await _cacheService.RemoveByPrefixAsync("playground", CancellationToken.None);

        return playground.Id;
    }
}
