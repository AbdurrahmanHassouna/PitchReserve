using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygroundById;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetOwnerPlaygroundById;

public sealed class GetOwnerPlaygroundByIdQueryHandler : IRequestHandler<GetOwnerPlaygroundByIdQuery, PlaygroundDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetOwnerPlaygroundByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PlaygroundDto> Handle(GetOwnerPlaygroundByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue || _currentUser.Role != "Owner")
            throw new ForbiddenAccessException("Only an owner can view their playgrounds.");

        var playground = await _context.Playgrounds.AsNoTracking()
            .Include(field => field.OwnerProfile)
            .FirstOrDefaultAsync(field => field.Id == request.Id && field.OwnerProfile.UserId == _currentUser.UserId.Value,
                cancellationToken);
        if (playground is null)
            throw new NotFoundException(nameof(Playground), request.Id);

        return new PlaygroundDto
        {
            Id = playground.Id,
            OwnerProfileId = playground.OwnerProfileId,
            OwnerBusinessName = playground.OwnerProfile.BusinessName,
            OnlinePaymentEnabled = playground.OwnerProfile.OnlinePaymentEnabled,
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
            Status = playground.Status,
            CreatedAt = playground.CreatedAt
        };
    }
}
