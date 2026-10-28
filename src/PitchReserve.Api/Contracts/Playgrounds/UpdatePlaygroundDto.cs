using PitchReserve.Domain.Enums;

namespace PitchReserve.Api.Contracts.Playgrounds;

public sealed record UpdatePlaygroundDto
{
    public string Name { get; init; } = string.Empty;
    public PitchSize Size { get; init; }
    public SurfaceType SurfaceType { get; init; }
    public decimal HourlyRate { get; init; }
    public string LocationCity { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public TimeOnly OpenHour { get; init; }
    public TimeOnly CloseHour { get; init; }
    public PlaygroundStatus Status { get; init; }
}
