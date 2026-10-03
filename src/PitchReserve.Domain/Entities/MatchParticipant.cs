using PitchReserve.Domain.Common;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Domain.Entities;

public class MatchParticipant : BaseEntity
{
    public Guid BookingId { get; private set; }
    public Guid PlayerId { get; private set; }
    public ParticipantStatus Status { get; private set; } = ParticipantStatus.Pending;
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;

    public Booking Booking { get; private set; } = null!;
    public User Player { get; private set; } = null!;

    private MatchParticipant() { }

    public static MatchParticipant Create(
        Guid bookingId,
        Guid playerId,
        ParticipantStatus status = ParticipantStatus.Pending)
    {
        if (bookingId == Guid.Empty)
            throw new DomainException("BookingId is required.");

        if (playerId == Guid.Empty)
            throw new DomainException("PlayerId is required.");

        return new MatchParticipant
        {
            BookingId = bookingId,
            PlayerId = playerId,
            Status = status,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void Accept()
    {
        if (Status == ParticipantStatus.Declined)
            throw new DomainException("Participant request has already been declined.");
        Status = ParticipantStatus.Joined;
    }

    public void Decline()
    {
        if (Status == ParticipantStatus.Joined)
            throw new DomainException("Participant request has already been accepted.");

        Status = ParticipantStatus.Declined;
    }

    public void Rejoin()
    {
        if(Status == ParticipantStatus.Joined)
            throw new DomainException("Participant request has already been Accepted.");
        Status = ParticipantStatus.Pending;
        JoinedAt = DateTime.UtcNow;
    }
    public void Remove()
    {
        if (Status != ParticipantStatus.Joined)
        {
            throw new DomainException("Participant request wasn't accepted.");
        }
        Status = ParticipantStatus.Declined;
    }
}
