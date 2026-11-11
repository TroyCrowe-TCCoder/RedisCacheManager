using Microsoft.Extensions.DependencyInjection;
using RedisCacheManager.Caching;

namespace RedisCacheManager.CacheConsumer.Extensions;

/// <summary>Registers the read-only cache-consumer wrapper into DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ICacheConsumer"/>, backed by the already-registered
    /// <see cref="ICacheManager"/> (e.g. from <c>AddRedisCacheManager</c>). Register
    /// <see cref="ICacheManager"/> before calling this method.
    /// </summary>
    public static IServiceCollection AddCacheConsumer(this IServiceCollection services)
    {
        services.AddSingleton<ICacheConsumer, RedisCacheConsumer>();
        return services;
    }
}
