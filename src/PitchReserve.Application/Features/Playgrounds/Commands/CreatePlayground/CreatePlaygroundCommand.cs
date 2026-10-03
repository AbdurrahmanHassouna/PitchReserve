using MediatR;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Playgrounds.Commands.CreatePlayground;

public record CreatePlaygroundCommand : IRequest<Guid>
{
    public Guid OwnerProfileId { get; init; }
    public string Name { get; init; } = string.Empty;
    public PitchSize Size { get; init; }
    public SurfaceType SurfaceType { get; init; }
    public decimal HourlyRate { get; init; }
    public string LocationCity { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public PlaygroundStatus Status { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public TimeOnly OpenHour { get; init; }
    public TimeOnly CloseHour { get; init; }
}
