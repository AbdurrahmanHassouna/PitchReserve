using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygroundById;

public record PlaygroundDto
{
    public Guid Id { get; init; }
    public Guid OwnerProfileId { get; init; }
    public string OwnerBusinessName { get; init; } = string.Empty;
    public bool OnlinePaymentEnabled { get; init; }
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
    public decimal AverageRating { get; init; }
    public int TotalRatings { get; init; }
    public DateTime CreatedAt { get; init; }
}
