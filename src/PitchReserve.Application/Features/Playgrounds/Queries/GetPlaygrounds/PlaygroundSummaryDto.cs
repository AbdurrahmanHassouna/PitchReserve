using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygrounds;

public record PlaygroundSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public PitchSize Size { get; init; }
    public SurfaceType SurfaceType { get; init; }
    public decimal HourlyRate { get; init; }
    public string LocationCity { get; init; } = string.Empty;
    public TimeOnly OpenHour { get; init; }
    public TimeOnly CloseHour { get; init; }
    public decimal AverageRating { get; init; }
}
