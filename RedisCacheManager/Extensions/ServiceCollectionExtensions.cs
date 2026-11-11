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
    /// Adds RedisCacheManager services. The host supplies every <see cref="RedisCacheOptions"/> value
    /// through <paramref name="configure"/>; the options are validated when the host starts.
    /// </summary>
    public static IServiceCollection AddRedisCacheManager(this IServiceCollection services, Action<RedisCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<RedisCacheOptions>()
            .Configure(configure)
            .Validate(o => !string.IsNullOrWhiteSpace(o.HostName), "RedisCacheOptions.HostName is required.")
            .Validate(o => o.Port > 0, "RedisCacheOptions.Port must be greater than zero.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.InstanceName), "RedisCacheOptions.InstanceName is required.")
            .Validate(o => string.IsNullOrWhiteSpace(o.KeyVaultUri) || !string.IsNullOrWhiteSpace(o.AccessKeySecretName), "RedisCacheOptions.AccessKeySecretName is required when KeyVaultUri is set.")
            .Validate(o => o.DefaultTtl > TimeSpan.Zero, "RedisCacheOptions.DefaultTtl must be greater than zero.")
            .Validate(o => o.MaxRetryAttempts >= 0, "RedisCacheOptions.MaxRetryAttempts must not be negative.")
            .Validate(o => o.RetryBaseDelay >= TimeSpan.Zero, "RedisCacheOptions.RetryBaseDelay must not be negative.")
            .Validate(o => o.ConnectTimeout > TimeSpan.Zero, "RedisCacheOptions.ConnectTimeout must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<RedisCacheMetrics>();
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<IRedisKeyBuilder, RedisKeyBuilder>();
        services.AddSingleton<ICacheManager, RedisCacheManagerService>();
        services.AddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
        services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");
        return services;
    }
}

