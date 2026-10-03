using PitchReserve.Domain.Common;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Domain.Entities;

public class Review : BaseEntity
{
    public Guid PlaygroundId { get; private set; }
    public Guid PlayerId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }

    public Playground Playground { get; private set; } = null!;
    public User Player { get; private set; } = null!;

    private Review() { }

    public static Review Create(Guid playgroundId, Guid playerId, int rating, string? comment = null)
    {
        if (playgroundId == Guid.Empty)
            throw new DomainException("PlaygroundId is required.");

        if (playerId == Guid.Empty)
            throw new DomainException("PlayerId is required.");

        if (rating < 1 || rating > 5)
            throw new DomainException("Rating must be between 1 and 5 stars.");

        return new Review
        {
            PlaygroundId = playgroundId,
            PlayerId = playerId,
            Rating = rating,
            Comment = comment?.Trim()
        };
    }

    public void UpdateReview(int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            throw new DomainException("Rating must be between 1 and 5 stars.");

        Rating = rating;
        Comment = comment?.Trim();
    }
}
