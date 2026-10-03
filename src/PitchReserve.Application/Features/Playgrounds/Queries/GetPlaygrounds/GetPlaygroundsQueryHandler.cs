using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Interfaces;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygrounds;

public class GetPlaygroundsQueryHandler :  IRequestHandler<GetPlaygroundsQuery, IList<PlaygroundSummaryDto>>
{
    private readonly ICacheService _cacheService;
    private readonly IApplicationDbContext _context;

    public GetPlaygroundsQueryHandler(ICacheService cacheService, IApplicationDbContext context)
    {
        _cacheService = cacheService;
        _context = context;
    }

    public async Task<IList<PlaygroundSummaryDto>> Handle(GetPlaygroundsQuery request, CancellationToken cancellationToken)
    {
        var key = "playgrounds";
        var cached = await _cacheService.GetAsync<IList<PlaygroundSummaryDto>>(key,cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var playgrounds = await _context.Playgrounds.Select(playground => new PlaygroundSummaryDto()
        {
            Id = playground.Id,
            SurfaceType =  playground.SurfaceType,
            AverageRating = playground.AverageRating,
            HourlyRate = playground.HourlyRate,
            LocationCity = playground.LocationCity,
            CloseHour = playground.CloseHour,
            OpenHour = playground.OpenHour,
            Name =  playground.Name,
            Size =  playground.Size
        }).ToListAsync(cancellationToken);

        await _cacheService.SetAsync(key, playgrounds,TimeSpan.FromMinutes(15), cancellationToken);
        return playgrounds;
    }
}