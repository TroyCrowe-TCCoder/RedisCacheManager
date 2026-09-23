using RedisCacheManager.Options;

namespace RedisCacheManager.Caching;

/// <summary>
/// Builds Redis keys using a consistent "{instance}:{category}:{id}" convention so that
/// multiple applications sharing the same Redis instance cannot collide with or evict
/// each other's keys.
/// </summary>
public sealed class RedisKeyBuilder : IRedisKeyBuilder
{
    private readonly string _instanceName;

    public RedisKeyBuilder(Microsoft.Extensions.Options.IOptions<RedisCacheOptions> options)
    {
        _instanceName = options.Value.InstanceName;
    }

    public string Build(string category, string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return $"{_instanceName}:{category}:{id}";
    }
}

