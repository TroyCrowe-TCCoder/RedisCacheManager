using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RedisCacheManager.Connection;
using RedisCacheManager.Options;
using RedisCacheManager.Telemetry;
using StackExchange.Redis;

namespace RedisCacheManager.Caching;

/// <summary>
/// Default <see cref="ICacheManager"/> implementation backed by StackExchange.Redis.
/// Transient failures are retried with exponential backoff via Polly; if all retries
/// are exhausted, the failure is logged and the operation reports a cache-miss to the
/// caller rather than throwing.
/// </summary>
public sealed class RedisCacheManagerService : ICacheManager
{
    private readonly IRedisConnectionProvider _connectionProvider;
    private readonly IRedisKeyBuilder _keyBuilder;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<RedisCacheManagerService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly RedisCacheMetrics _metrics;

    public RedisCacheManagerService(
        IRedisConnectionProvider connectionProvider,
        IRedisKeyBuilder keyBuilder,
        IOptions<RedisCacheOptions> options,
        ILogger<RedisCacheManagerService> logger,
        RedisCacheMetrics metrics)
    {
        _connectionProvider = connectionProvider;
        _keyBuilder = keyBuilder;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = _options.RetryBaseDelay,
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<RedisException>().Handle<RedisTimeoutException>(),
            })
            .Build();
    }

    public async Task<(bool Found, T? Value)> TryGetAsync<T>(string category, string id, CancellationToken cancellationToken = default)
    {
        var key = _keyBuilder.Build(category, id);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            RedisValue result = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var connection = await _connectionProvider.GetConnectionAsync(ct).ConfigureAwait(false);
                var database = connection.GetDatabase();
                return await database.StringGetAsync(key).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            if (result.IsNullOrEmpty)
            {
                _metrics.CacheMisses.Add(1, new KeyValuePair<string, object?>("operation", "get"));
                return (false, default);
            }

            var value = JsonSerializer.Deserialize<T>((string)result!);
            _metrics.CacheHits.Add(1, new KeyValuePair<string, object?>("operation", "get"));
            return (true, value);
        }
        catch (Exception ex)
        {
            _metrics.CacheErrors.Add(1, new KeyValuePair<string, object?>("operation", "get"));
            _metrics.CacheMisses.Add(1, new KeyValuePair<string, object?>("operation", "get"));
            _logger.LogWarning(ex, "Redis read failed for key {CacheKey}; treating as cache-miss.", key);
            return (false, default);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.OperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "get"));
        }
    }

    public async Task SetAsync<T>(string category, string id, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var key = _keyBuilder.Build(category, id);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var connection = await _connectionProvider.GetConnectionAsync(ct).ConfigureAwait(false);
                var database = connection.GetDatabase();
                await database.StringSetAsync(key, serialized, ttl ?? _options.DefaultTtl).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
            _metrics.CacheSets.Add(1, new KeyValuePair<string, object?>("operation", "set"));
        }
        catch (Exception ex)
        {
            _metrics.CacheErrors.Add(1, new KeyValuePair<string, object?>("operation", "set"));
            _logger.LogWarning(ex, "Redis write failed for key {CacheKey}; value was not cached.", key);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.OperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "set"));
        }
    }

    public async Task RemoveAsync(string category, string id, CancellationToken cancellationToken = default)
    {
        var key = _keyBuilder.Build(category, id);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var connection = await _connectionProvider.GetConnectionAsync(ct).ConfigureAwait(false);
                var database = connection.GetDatabase();
                await database.KeyDeleteAsync(key).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
            _metrics.CacheRemovals.Add(1, new KeyValuePair<string, object?>("operation", "remove"));
        }
        catch (Exception ex)
        {
            _metrics.CacheErrors.Add(1, new KeyValuePair<string, object?>("operation", "remove"));
            _logger.LogWarning(ex, "Redis eviction failed for key {CacheKey}.", key);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.OperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "remove"));
        }
    }
}

