using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Common.Services;

public sealed class BookingScheduleValidator : IBookingScheduleValidator
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly TimeZoneInfo _fieldTimeZone;

    public BookingScheduleValidator(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _fieldTimeZone = ResolveEgyptTimeZone();
    }

    public async Task<Playground> ValidateAsync(
        Guid fieldId,
        DateTimeOffset start,
        DateTimeOffset end,
        Guid? excludedBookingId,
        CancellationToken token)
    {
        if (fieldId == Guid.Empty)
            throw ValidationError(nameof(fieldId), "Field ID is required.");

        ValidateInterval(start, end);

        var playground = await _context.Playgrounds
            .AsNoTracking()
            .FirstOrDefaultAsync(field => field.Id == fieldId, token);
        if (playground is null)
            throw new NotFoundException(nameof(Playground), fieldId);

        if (playground.Status != PlaygroundStatus.Opened)
            throw new ConflictException("This field is not open for bookings.");

        if (!ValidateOpeningWindow(start, end, playground.OpenHour, playground.CloseHour))
            throw ValidationError(nameof(start), "The selected time must fit within the field's opening hours.");

        var now = _dateTimeProvider.UtcNow;
        var startUtc = start.UtcDateTime;
        var endUtc = end.UtcDateTime;
        var hasCollision = await _context.Bookings.AnyAsync(booking =>
                booking.PlaygroundId == fieldId &&
                booking.Status != BookingStatus.Cancelled &&
                (!excludedBookingId.HasValue || booking.Id != excludedBookingId.Value) &&
                booking.StartTime < endUtc &&
                booking.EndTime > startUtc &&
                (!booking.ExpiresAt.HasValue || booking.ExpiresAt.Value > now),
            token);

        if (hasCollision)
        {
            throw new ConflictException(
                $"The selected time slot ({start:yyyy-MM-dd HH:mm} - {end:HH:mm}) is already booked for this field.");
        }

        return playground;
    }

    public bool ValidateOpeningWindow(DateTimeOffset start, DateTimeOffset end, TimeOnly open, TimeOnly close)
    {
        if (open == close || end <= start)
            return false;

        var startLocal = TimeZoneInfo.ConvertTime(start, _fieldTimeZone).DateTime;
        var endLocal = TimeZoneInfo.ConvertTime(end, _fieldTimeZone).DateTime;

        var startDate = DateOnly.FromDateTime(startLocal);
        // A window may start on the previous local day when it crosses midnight. shift oriented 
        return IsInsideWindow(startDate, startLocal, endLocal, open, close) ||
               IsInsideWindow(startDate.AddDays(-1), startLocal, endLocal, open, close);
    }

    private static bool IsInsideWindow(
        DateOnly anchorDate,
        DateTime startLocal,
        DateTime endLocal,
        TimeOnly open,
        TimeOnly close)
    {
        var anchor = anchorDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localOpen = anchor.Add(open.ToTimeSpan());
        var localClose = anchor.Add(close.ToTimeSpan());
        if (close <= open)
            localClose = localClose.AddDays(1);

        return startLocal >= localOpen &&
               startLocal < localClose &&
               endLocal <= localClose;
    }

    private void ValidateInterval(DateTimeOffset start, DateTimeOffset end)
    {
        if (start == default)
            throw ValidationError("StartTime", "Start time is required.");
        if (end == default || end <= start)
            throw ValidationError("EndTime", "End time must be after start time.");

        var now = _dateTimeProvider.UtcNow;
        if (start.UtcDateTime <= now)
            throw ValidationError("StartTime", "Start time must be in the future.");

        if ((end - start).Ticks % TimeSpan.TicksPerHour != 0)
            throw ValidationError("EndTime", "Bookings must have a whole-hour duration.");

        var localStart = TimeZoneInfo.ConvertTime(start, _fieldTimeZone).DateTime;
        var localEnd = TimeZoneInfo.ConvertTime(end, _fieldTimeZone).DateTime;
        if (!IsWholeHour(localStart) || !IsWholeHour(localEnd))
            throw ValidationError("StartTime", "Booking times must start and end on local whole-hour boundaries.");
    }

    private static bool IsWholeHour(DateTime localTime) =>
        localTime.Minute == 0 && localTime.Second == 0 && localTime.Millisecond == 0 &&
        localTime.Ticks % TimeSpan.TicksPerSecond == 0;

    private static ValidationException ValidationError(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static TimeZoneInfo ResolveEgyptTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
    }
}
