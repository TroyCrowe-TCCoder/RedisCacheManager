namespace RedisCacheManager.CacheConsumer;

/// <summary>
/// Read-only cache abstraction for consumers that must never write or evict cache
/// entries — only the upstream owner of the data may do that. Wraps
/// <c>ICacheManager</c> from <c>TCCoder.RedisCacheManager</c> but intentionally
/// exposes only the read path.
/// </summary>
public interface ICacheConsumer
{
    /// <summary>
    /// Attempts to read a cached value. Returns false if the key does not exist or the
    /// operation could not be completed (e.g. Redis unavailable); the caller should fall
    /// back to its own source of truth in either case.
    /// </summary>
    Task<(bool Found, T? Value)> TryGetAsync<T>(string category, string id, CancellationToken cancellationToken = default);
}
