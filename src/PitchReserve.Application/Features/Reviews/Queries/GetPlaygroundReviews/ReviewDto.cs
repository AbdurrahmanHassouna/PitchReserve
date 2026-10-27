namespace PitchReserve.Application.Features.Reviews.Queries.GetPlaygroundReviews;

public sealed record ReviewDto
{
    public Guid Id { get; init; }
    public string AuthorDisplayName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
