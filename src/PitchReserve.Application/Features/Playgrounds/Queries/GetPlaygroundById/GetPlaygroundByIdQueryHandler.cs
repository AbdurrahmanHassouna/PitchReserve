using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygroundById;

public class GetPlaygroundByIdQueryHandler : IRequestHandler<GetPlaygroundByIdQuery, PlaygroundDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public GetPlaygroundByIdQueryHandler(IApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<PlaygroundDto> Handle(GetPlaygroundByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"playground:{request.Id}";

        var cachedPlayground = await _cacheService.GetAsync<PlaygroundDto>(cacheKey, cancellationToken);
        if (cachedPlayground is not null)
        {
            return cachedPlayground;
        }

        var playground = await _context.Playgrounds
            .AsNoTracking()
            .Include(p => p.OwnerProfile)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (playground is null)
        {
            throw new NotFoundException(nameof(Playground), request.Id);
        }

        var dto = new PlaygroundDto
        {
            Id = playground.Id,
            OwnerProfileId = playground.OwnerProfileId,
            OwnerBusinessName = playground.OwnerProfile?.BusinessName ?? string.Empty,
            Name = playground.Name,
            Size = playground.Size,
            SurfaceType = playground.SurfaceType,
            HourlyRate = playground.HourlyRate,
            LocationCity = playground.LocationCity,
            Address = playground.Address,
            Latitude = playground.Latitude,
            Longitude = playground.Longitude,
            OpenHour = playground.OpenHour,
            CloseHour = playground.CloseHour,
            AverageRating = playground.AverageRating,
            TotalRatings = playground.TotalRatings,
            CreatedAt = playground.CreatedAt,
            OnlinePaymentEnabled = playground.OwnerProfile.OnlinePaymentEnabled
        };

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30), cancellationToken);

        return dto;
    }
}
