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
    public BookingStatus Status { get; private set; } = BookingStatus.PendingOwnerApproval;
    public DateTime? ExpiresAt { get; private set; }
    public bool IsOpenForPublicPlayers { get; private set; }
    public int? MaxPlayersNeeded { get; private set; }
    public int JoinedParticipantsCount { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public Playground Playground { get; private set; } = null!;
    public User OrganizerPlayer { get; private set; } = null!;
    public Payment Payment { get; private set; } = null!;
    public ICollection<MatchParticipant> MatchParticipants { get; private set; } = new List<MatchParticipant>();

    private Booking()
    {
    }

    // Kept for existing domain callers. New application code should provide its injected clock.
    public static Booking Create(
        Guid playgroundId,
        Guid organizerPlayerId,
        DateTime startTime,
        DateTime endTime,
        bool isOpenForPublicPlayers = false,
        int? maxPlayersNeeded = null,
        DateTime? expiresAt = null) =>
        Create(playgroundId, organizerPlayerId, startTime, endTime, isOpenForPublicPlayers,
            maxPlayersNeeded, expiresAt, DateTime.UtcNow);

    public static Booking Create(
        Guid playgroundId,
        Guid organizerPlayerId,
        DateTime startTime,
        DateTime endTime,
        bool isOpenForPublicPlayers,
        int? maxPlayersNeeded,
        DateTime? expiresAt,
        DateTime utcNow)
    {
        if (playgroundId == Guid.Empty)
            throw new DomainException("PlaygroundId is required.");

        if (organizerPlayerId == Guid.Empty)
            throw new DomainException("OrganizerPlayerId is required.");

        startTime = AsUtc(startTime);
        endTime = AsUtc(endTime);
        utcNow = AsUtc(utcNow);
        expiresAt = expiresAt.HasValue ? AsUtc(expiresAt.Value) : null;

        if (startTime >= endTime)
            throw new DomainException("Start time must be before end time.");

        if (startTime <= utcNow)
            throw new DomainException("Date must be in the future.");

        var booking = new Booking
        {
            PlaygroundId = playgroundId,
            OrganizerPlayerId = organizerPlayerId,
            BookingTime = utcNow,
            StartTime = startTime,
            EndTime = endTime,
            ExpiresAt = expiresAt,
            JoinedParticipantsCount = 1
        };
        booking.ConfigurePublicMatch(isOpenForPublicPlayers, maxPlayersNeeded);
        return booking;
    }

    public void AcceptByOwner(DateTime utcNow)
    {
        if (Status is BookingStatus.AcceptedAwaitingCash or BookingStatus.Confirmed)
            return;
        var now = AsUtc(utcNow);
        if (StartTime <= now)
            throw new ConflictException("Cannot accept a booking after kickoff.");
        if (Status is BookingStatus.Cancelled)
            throw new ConflictException("Cannot accept a cancelled booking.");
        if (ExpiresAt.HasValue && ExpiresAt.Value <= now)
            throw new ConflictException("Cannot accept an expired booking hold.");

        Status = BookingStatus.AcceptedAwaitingCash;
        ExpiresAt = null;
    }

    public void Confirm()
    {
        if (Status == BookingStatus.Confirmed)
            return;
        if (Status != BookingStatus.AcceptedAwaitingCash)
            throw new ConflictException("The owner must accept the booking before cash can confirm it.");

        Status = BookingStatus.Confirmed;
        ExpiresAt = null;
    }

    public void Cancel(Guid actorUserId, DateTime utcNow, string? reason)
    {
        if (Status == BookingStatus.Cancelled)
            return;
        if (actorUserId == Guid.Empty)
            throw new DomainException("Cancellation actor is required.");

        utcNow = AsUtc(utcNow);
        if (utcNow > StartTime.AddHours(-24))
            throw new ConflictException("A booking can only be cancelled at least 24 hours before it starts.");

        Status = BookingStatus.Cancelled;
        CancelledAt = utcNow;
        CancelledByUserId = actorUserId;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void Reschedule(DateTime startTime, DateTime endTime, DateTime utcNow)
    {
        utcNow = AsUtc(utcNow);
        startTime = AsUtc(startTime);
        endTime = AsUtc(endTime);

        if (Status == BookingStatus.Cancelled)
            throw new ConflictException("A cancelled booking cannot be rescheduled.");
        if (StartTime <= utcNow)
            throw new ConflictException("A booking can only be rescheduled before kickoff.");
        if (ExpiresAt.HasValue && ExpiresAt.Value <= utcNow)
            throw new ConflictException("An expired booking hold cannot be rescheduled.");
        if (endTime <= startTime)
            throw new DomainException("End time must be after start time.");
        if (endTime - startTime != EndTime - StartTime)
            throw new DomainException("Rescheduling must preserve the booking duration.");
        if (startTime <= utcNow)
            throw new DomainException("The new start time must be in the future.");

        StartTime = startTime;
        EndTime = endTime;
    }

    public void UpdateHoldExpiration(DateTime? newExpiresAt)
    {
        ExpiresAt = newExpiresAt.HasValue ? AsUtc(newExpiresAt.Value) : null;
    }

    public void ConfigurePublicMatch(bool isOpen, int? maxPlayersNeeded = null)
    {
        IsOpenForPublicPlayers = isOpen;
        if (!isOpen)
        {
            MaxPlayersNeeded = null;
            return;
        }

        if (!maxPlayersNeeded.HasValue || maxPlayersNeeded.Value < 1)
            throw new DomainException("Max players needed must be at least one for a public match.");

        MaxPlayersNeeded = maxPlayersNeeded;
    }

    public void UpdateMatchSettings(bool isOpenForPublicPlayers, int? maxPlayersNeeded, DateTime utcNow)
    {
        utcNow = AsUtc(utcNow);
        if (Status is not (BookingStatus.PendingOwnerApproval or BookingStatus.AcceptedAwaitingCash or BookingStatus.Confirmed))
            throw new ConflictException("Match settings can only be changed while the booking is pending or accepted.");
        if (StartTime <= utcNow || ExpiresAt.HasValue && ExpiresAt.Value <= utcNow)
            throw new ConflictException("Match settings can only be changed before kickoff on an active booking.");
        if (isOpenForPublicPlayers && (!maxPlayersNeeded.HasValue || maxPlayersNeeded.Value < 1))
            throw new DomainException("Max players needed must be at least one for a public match.");
        if (isOpenForPublicPlayers && maxPlayersNeeded!.Value < JoinedParticipantsCount)
            throw new ConflictException("Max players needed cannot be less than the number of joined participants.");

        ConfigurePublicMatch(isOpenForPublicPlayers, maxPlayersNeeded);
    }

    public void IncreaseAcceptedParticipant(DateTime utcNow, bool isOrganizerRejoin = false)
    {
        EnsureParticipationIsOpen(utcNow);

        if (!isOrganizerRejoin && (!IsOpenForPublicPlayers || !MaxPlayersNeeded.HasValue))
            throw new ConflictException("This is a private booking.");

        if (MaxPlayersNeeded.HasValue && JoinedParticipantsCount >= MaxPlayersNeeded.Value)
            throw new ConflictException("Booking is already full.");

        JoinedParticipantsCount++;
    }

    public void AddAcceptedParticipant(DateTime utcNow, bool isOrganizerRejoin = false) =>
        IncreaseAcceptedParticipant(utcNow, isOrganizerRejoin);

    public void DecreaseAcceptedParticipant(DateTime utcNow)
    {
        EnsureParticipationIsOpen(utcNow);
        if (JoinedParticipantsCount <= 0)
            throw new ConflictException("Booking has no accepted participants to remove.");

        JoinedParticipantsCount--;
    }

    public void RemoveAcceptedParticipant(DateTime utcNow) =>
        DecreaseAcceptedParticipant(utcNow);

    private void EnsureParticipationIsOpen(DateTime utcNow)
    {
        utcNow = AsUtc(utcNow);
        if (Status is not (BookingStatus.AcceptedAwaitingCash or BookingStatus.Confirmed))
            throw new ConflictException("The field owner must accept the booking before participation can change.");
        if (StartTime <= utcNow)
            throw new ConflictException("Participation changes are closed after kickoff.");
        if (Status == BookingStatus.Cancelled)
            throw new ConflictException("Participation cannot change on a cancelled booking.");
        if (ExpiresAt.HasValue && ExpiresAt.Value <= utcNow)
            throw new ConflictException("Participation cannot change on an expired booking.");
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
