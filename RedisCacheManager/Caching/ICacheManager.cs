namespace RedisCacheManager.Caching;

/// <summary>
/// Shared cache abstraction for reading and writing values in Redis. All methods treat
/// both a true cache-miss and a Redis failure (connection error, timeout, etc.) the same
/// way for the caller: a "not found" result. Failures are logged internally by the
/// implementation but never thrown to the caller, so consumers should always be prepared
/// to fall back to the source of truth (e.g. a database call) when a value is not returned.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Attempts to read a cached value. Returns false if the key does not exist or the
    /// operation could not be completed (e.g. Redis unavailable); the caller should fall
    /// back to its own source of truth in either case.
    /// </summary>
    Task<(bool Found, T? Value)> TryGetAsync<T>(string category, string id, CancellationToken cancellationToken = default);

    /// <summary>Writes a value to the cache. Failures are logged and swallowed; they never propagate to the caller.</summary>
    Task SetAsync<T>(string category, string id, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>Removes a value from the cache, e.g. on explicit invalidation such as a role change.</summary>
    Task RemoveAsync(string category, string id, CancellationToken cancellationToken = default);
}

