using RedisCacheManager.Caching;

namespace RedisCacheManager.CacheConsumer;

/// <summary>
/// Default <see cref="ICacheConsumer"/> implementation that forwards reads to an
/// injected <see cref="ICacheManager"/> and never calls its write or remove members.
/// </summary>
public sealed class RedisCacheConsumer : ICacheConsumer
{
    private readonly ICacheManager _cacheManager;

    public RedisCacheConsumer(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
    }

    /// <inheritdoc />
    public Task<(bool Found, T? Value)> TryGetAsync<T>(string category, string id, CancellationToken cancellationToken = default)
        => _cacheManager.TryGetAsync<T>(category, id, cancellationToken);
}
