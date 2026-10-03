using PitchReserve.Domain.Common;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid PlaygroundId { get; private set; }
    public Guid OrganizerPlayerId { get; private set; }
    public DateTime BookingTime { get; private set; } = DateTime.UtcNow;
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.PendingPayment;
    public DateTime? ExpiresAt { get; private set; }
    public bool IsOpenForPublicPlayers { get; private set; } = false;
    public int? MaxPlayersNeeded { get; private set; }
    public int JoinedParticipantsCount { get; private set; } = 0;
    public Playground Playground { get; private set; } = null!;
    public User OrganizerPlayer { get; private set; } = null!;
    public Payment Payment { get; private set; }
    public ICollection<MatchParticipant> MatchParticipants { get; private set; } = new List<MatchParticipant>();

    private Booking()
    {
    }

    public static Booking Create(
        Guid playgroundId,
        Guid organizerPlayerId,
        DateTime startTime,
        DateTime endTime,
        bool isOpenForPublicPlayers = false,
        int? maxPlayersNeeded = null,
        DateTime? expiresAt = null)
    {
        if (playgroundId == Guid.Empty)
            throw new DomainException("PlaygroundId is required.");

        if (organizerPlayerId == Guid.Empty)
            throw new DomainException("OrganizerPlayerId is required.");

        if (startTime >= endTime)
            throw new DomainException("Start time must be before end time.");

        if (startTime <= DateTime.UtcNow)
            throw new DomainException("Date must be in the future.");
        var booking = new Booking
        {
            PlaygroundId = playgroundId,
            OrganizerPlayerId = organizerPlayerId,
            StartTime = startTime,
            EndTime = endTime,
            ExpiresAt = expiresAt
        };
        booking.ConfigurePublicMatch(isOpenForPublicPlayers, maxPlayersNeeded);
        return booking;
    }

    public void Confirm()
    {
        if (StartTime <= DateTime.UtcNow)
            throw new DomainException("Cannot confirm old booking");
        if (Status is BookingStatus.Cancelled)
            throw new DomainException("Cannot confirm a cancelled booking.");

        Status = BookingStatus.Confirmed;
        ExpiresAt = null;
    }

    public void Cancel()
    {
        if (EndTime <= DateTime.UtcNow)
            throw new DomainException("Cannot cancel a completed booking.");

        Status = BookingStatus.Cancelled;
    }

    public void UpdateHoldExpiration(DateTime? newExpiresAt)
    {
        ExpiresAt = newExpiresAt;
    }
    public void ConfigurePublicMatch(bool isOpen, int? maxPlayersNeeded = null)
    {
        IsOpenForPublicPlayers = isOpen;
        if (!isOpen) return;
        if (!maxPlayersNeeded.HasValue)
            throw new DomainException("Max players needed is required.");
        MaxPlayersNeeded = maxPlayersNeeded;
    }

    public void IncrementMatchParticipant()
    {
        if (EndTime <= DateTime.UtcNow || StartTime <= DateTime.UtcNow)
            throw new DomainException("Can't Join old Booking.");
        if (!MaxPlayersNeeded.HasValue || !IsOpenForPublicPlayers)
            throw new DomainException("This is private Booking");
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("Can't join Canceled Booking.");
        if (ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow)
            throw new DomainException("Can't join Expired Booking.");
        if (JoinedParticipantsCount + 1 > MaxPlayersNeeded)
            throw new DomainException("Booking is already full.");
        JoinedParticipantsCount++;
    }

    public void DecrementMatchParticipant()
    {
        if (EndTime <= DateTime.UtcNow || StartTime <= DateTime.UtcNow)
            throw new DomainException("This is Expired Booking.");
        if (!MaxPlayersNeeded.HasValue || !IsOpenForPublicPlayers)
            throw new DomainException("This is private Booking");
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("This is Canceled Booking.");
        if (ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow)
            throw new DomainException("This is Expired Booking.");
        if (JoinedParticipantsCount == 0)
        {
            throw new DomainException("Booking is already empty.");
        }

        JoinedParticipantsCount--;
    }
}