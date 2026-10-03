using PitchReserve.Application.Common.Interfaces;

namespace PitchReserve.Infrastructure.Common;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
