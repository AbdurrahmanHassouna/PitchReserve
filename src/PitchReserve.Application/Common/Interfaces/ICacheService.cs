namespace PitchReserve.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default);
    Task<bool> TryAcquireLockAsync(string key, string token, TimeSpan expiration,
        CancellationToken cancellationToken = default);
    Task<bool> ExtendLockAsync(string key, string token, TimeSpan expiration,
        CancellationToken cancellationToken = default);
    Task<bool> ReleaseLockAsync(string key, string token,
        CancellationToken cancellationToken = default);
}
