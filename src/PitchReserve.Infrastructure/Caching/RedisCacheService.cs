using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Interfaces;
using StackExchange.Redis;

namespace PitchReserve.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public Task<bool> TryAcquireLockAsync(string key, string token, TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        ValidateLockArguments(key, token);
        ValidateExpiration(expiration);
        cancellationToken.ThrowIfCancellationRequested();

        return _connectionMultiplexer.GetDatabase().LockTakeAsync(key, token, expiration);
    }

    public Task<bool> ExtendLockAsync(string key, string token, TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        ValidateLockArguments(key, token);
        ValidateExpiration(expiration);
        cancellationToken.ThrowIfCancellationRequested();

        return _connectionMultiplexer.GetDatabase().LockExtendAsync(key, token, expiration);
    }

    public Task<bool> ReleaseLockAsync(string key, string token,
        CancellationToken cancellationToken = default)
    {
        ValidateLockArguments(key, token);
        cancellationToken.ThrowIfCancellationRequested();

        return _connectionMultiplexer.GetDatabase().LockReleaseAsync(key, token);
    }

    private static void ValidateLockArguments(string key, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
    }

    private static void ValidateExpiration(TimeSpan expiration)
    {
        if (expiration < TimeSpan.FromMilliseconds(1))
        {
            throw new ArgumentOutOfRangeException(nameof(expiration),
                "Lock expiration must be at least one millisecond.");
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedData, SerializerOptions);
        }
        catch (Exception e)
        {
            _logger.LogError("Cache retrieval failed with {ExceptionType}: {StackTrace}", e.GetType().FullName, e.StackTrace);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null
        , CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                ValidateExpiration(expiration.Value);
                options.SetAbsoluteExpiration(expiration.Value);
            }

            var serialized = JsonSerializer.Serialize(value, SerializerOptions);
            await _cache.SetStringAsync(key, serialized, options, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Cache write failed with {ExceptionType}: {StackTrace}", e.GetType().FullName, e.StackTrace);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Cache removal failed with {ExceptionType}: {StackTrace}", e.GetType().FullName, e.StackTrace);
        }

    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefixKey))
        {
            return;
        }

        if (!_connectionMultiplexer.IsConnected)
        {
            return;
        }

        try
        {
            var database = _connectionMultiplexer.GetDatabase();


            foreach (var endpoint in _connectionMultiplexer.GetEndPoints())
            {
                var server = _connectionMultiplexer.GetServer(endpoint);
                if (server.IsConnected)
                {
                    var keys =  server.KeysAsync(pattern: $"{prefixKey}*");
                    var keyBatch = new List<RedisKey>();
                    await foreach (var key in keys.WithCancellation(cancellationToken))
                    {
                        keyBatch.Add(key);
                        if (keyBatch.Count >= 250)
                        {
                            await database.KeyDeleteAsync(keyBatch.ToArray());
                            keyBatch.Clear();
                        }
                    }

                    if (keyBatch.Count > 0)
                    {
                        await database.KeyDeleteAsync(keyBatch.ToArray());
                    }

                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Cache prefix removal failed with {ExceptionType}: {StackTrace}", e.GetType().FullName, e.StackTrace);
        }
    }
}
