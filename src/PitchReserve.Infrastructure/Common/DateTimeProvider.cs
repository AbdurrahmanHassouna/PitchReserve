using PitchReserve.Application.Common.Interfaces;

namespace PitchReserve.Infrastructure.Common;
// this to enable fake tests
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
