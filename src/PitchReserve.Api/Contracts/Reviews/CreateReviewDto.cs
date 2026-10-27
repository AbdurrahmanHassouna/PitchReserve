namespace PitchReserve.Api.Contracts.Reviews;

public sealed record CreateReviewDto
{
    public int Rating { get; init; }
    public string? Comment { get; init; }
}
