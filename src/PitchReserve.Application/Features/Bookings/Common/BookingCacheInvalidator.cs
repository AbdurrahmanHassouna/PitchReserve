using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Interfaces;

namespace PitchReserve.Application.Features.Bookings.Common;

internal static class BookingCacheInvalidator
{
    public static async Task InvalidateAsync(
        ICacheService cache,
        ILogger logger,
        Guid? playgroundId = null)
    {
        await TryRemoveAsync(cache, logger, "bookings");
        if (!playgroundId.HasValue)
            return;

        await TryRemoveAsync(cache, logger, "playgrounds");
        await TryRemoveAsync(cache, logger, $"playground:{playgroundId.Value:D}");
    }

    private static async Task TryRemoveAsync(ICacheService cache, ILogger logger, string prefix)
    {
        try
        {
            await cache.RemoveByPrefixAsync(prefix, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Could not invalidate cached booking or field data for prefix {CachePrefix}; exception type {ExceptionType}.",
                prefix,
                exception.GetType().Name);
        }
    }
}
