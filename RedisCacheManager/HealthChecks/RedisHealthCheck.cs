using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedisCacheManager.Connection;

namespace RedisCacheManager.HealthChecks;

/// <summary>ASP.NET Core health check that verifies the shared Redis connection responds to a PING.</summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IRedisConnectionProvider _connectionProvider;

    public RedisHealthCheck(IRedisConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await _connectionProvider.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            var database = connection.GetDatabase();
            var latency = await database.PingAsync().ConfigureAwait(false);
            return HealthCheckResult.Healthy($"Redis responded in {latency.TotalMilliseconds}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis connection is unavailable.", ex);
        }
    }
}

