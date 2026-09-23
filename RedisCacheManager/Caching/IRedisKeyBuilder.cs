namespace RedisCacheManager.Caching;

/// <summary>Builds conventioned Redis keys to avoid collisions between applications sharing the same Redis instance.</summary>
public interface IRedisKeyBuilder
{
    /// <summary>Builds a key in the form "{instance}:{category}:{id}".</summary>
    string Build(string category, string id);
}

