using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using RedisCacheManager.Caching;
using RedisCacheManager.Connection;
using RedisCacheManager.HealthChecks;
using RedisCacheManager.Locking;
using RedisCacheManager.Options;
using RedisCacheManager.Telemetry;

namespace RedisCacheManager.Extensions;

/// <summary>Registers RedisCacheManager services (connection, cache manager, key builder, distributed lock, health check) into DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds RedisCacheManager services, binding <see cref="RedisCacheOptions"/> from the
    /// "RedisCacheManager" configuration section (or the section named by
    /// <see cref="RedisCacheOptions.SectionName"/>).
    /// </summary>
    public static IServiceCollection AddRedisCacheManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisCacheOptions>(configuration.GetSection(RedisCacheOptions.SectionName));
        services.AddSingleton<RedisCacheMetrics>();
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<IRedisKeyBuilder, RedisKeyBuilder>();
        services.AddSingleton<ICacheManager, RedisCacheManagerService>();
        services.AddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
        services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");
        return services;
    }
}

