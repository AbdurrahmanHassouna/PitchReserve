using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygrounds;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetOwnerPlaygrounds;

public sealed class GetOwnerPlaygroundsQueryHandler : IRequestHandler<GetOwnerPlaygroundsQuery, PagedResult<PlaygroundSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetOwnerPlaygroundsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<PlaygroundSummaryDto>> Handle(GetOwnerPlaygroundsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue || _currentUser.Role != "Owner")
            throw new ForbiddenAccessException("Only an owner can view their playgrounds.");

        var query = _context.Playgrounds.AsNoTracking()
            .Where(playground => playground.OwnerProfile.UserId == _currentUser.UserId.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min(((long)request.Page - 1) * request.PageSize, int.MaxValue);
        var items = await query
            .OrderByDescending(playground => playground.CreatedAt)
            .ThenBy(playground => playground.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(playground => new PlaygroundSummaryDto
            {
                Id = playground.Id,
                Name = playground.Name,
                Size = playground.Size,
                SurfaceType = playground.SurfaceType,
                HourlyRate = playground.HourlyRate,
                LocationCity = playground.LocationCity,
                OpenHour = playground.OpenHour,
                CloseHour = playground.CloseHour,
                AverageRating = playground.AverageRating,
                Status = playground.Status
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PlaygroundSummaryDto>(items, request.Page, request.PageSize, totalCount);
    }
}
