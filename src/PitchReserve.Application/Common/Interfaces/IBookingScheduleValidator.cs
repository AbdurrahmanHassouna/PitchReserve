using PitchReserve.Domain.Entities;

namespace PitchReserve.Application.Common.Interfaces;

public interface IBookingScheduleValidator
{
    Task<Playground> ValidateAsync(
        Guid fieldId,
        DateTimeOffset start,
        DateTimeOffset end,
        Guid? excludedBookingId,
        CancellationToken token);

    bool ValidateOpeningWindow(DateTimeOffset start, DateTimeOffset end, TimeOnly open, TimeOnly close);
}
