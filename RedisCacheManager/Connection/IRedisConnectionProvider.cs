using StackExchange.Redis;

namespace RedisCacheManager.Connection;

/// <summary>Provides access to the shared Redis connection multiplexer.</summary>
public interface IRedisConnectionProvider
{
    /// <summary>Gets a connected <see cref="IConnectionMultiplexer"/>, establishing the connection if necessary.</summary>
    Task<IConnectionMultiplexer> GetConnectionAsync(CancellationToken cancellationToken = default);
}

