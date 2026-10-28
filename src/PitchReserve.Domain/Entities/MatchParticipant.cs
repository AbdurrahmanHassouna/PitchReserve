using System.Data;
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

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public Booking Booking { get; private set; } = null!;
    public User Player { get; private set; } = null!;

    private MatchParticipant() { }
    public static MatchParticipant Create(
        Guid bookingId,
        Guid playerId,
        DateTime utcNow,
        ParticipantStatus status = ParticipantStatus.Pending)
    {
        if (bookingId == Guid.Empty)
            throw new DomainException("BookingId is required.");

        if (playerId == Guid.Empty)
            throw new DomainException("PlayerId is required.");

        if (!Enum.IsDefined(status))
            throw new DomainException("Participant status is invalid.");

        return new MatchParticipant
        {
            BookingId = bookingId,
            PlayerId = playerId,
            Status = status,
            JoinedAt = AsUtc(utcNow)
        };
    }

    public static MatchParticipant Create(
        Guid bookingId,
        Guid playerId,
        ParticipantStatus status,
        DateTime utcNow) =>
        Create(bookingId, playerId, utcNow, status);

    public void Accept()
    {
        if (Status == ParticipantStatus.Joined)
            return;
        if (Status != ParticipantStatus.Pending)
            throw new ConflictException("Only a pending participant request can be accepted.");

        Status = ParticipantStatus.Joined;
    }

    public void Decline()
    {
        if (Status == ParticipantStatus.Declined)
            return;
        if (Status != ParticipantStatus.Pending)
            throw new ConflictException("Only a pending participant request can be declined.");

        Status = ParticipantStatus.Declined;
    }

    public void ResubmitDeclinedRequest(DateTime utcNow)
    {
        if (Status == ParticipantStatus.Pending)
            return;
        if (Status != ParticipantStatus.Declined)
            throw new ConflictException("This participant cannot submit another request for the match.");

        Status = ParticipantStatus.Pending;
        JoinedAt = AsUtc(utcNow);
    }

    public void Withdraw()
    {
        if (Status == ParticipantStatus.Withdrawn)
            return;
        if (Status != ParticipantStatus.Joined)
            throw new ConflictException("Only an accepted participant can withdraw.");

        Status = ParticipantStatus.Withdrawn;
    }

    public void RejoinAsOrganizer(DateTime utcNow)
    {
        if (Status == ParticipantStatus.Joined)
            return;
        if (Status != ParticipantStatus.Withdrawn)
            throw new ConflictException("Only a withdrawn organizer can rejoin.");

        Status = ParticipantStatus.Joined;
        JoinedAt = AsUtc(utcNow);
    }


    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
