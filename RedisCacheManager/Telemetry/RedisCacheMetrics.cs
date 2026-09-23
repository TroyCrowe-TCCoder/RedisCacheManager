using System.Diagnostics.Metrics;

namespace RedisCacheManager.Telemetry;

/// <summary>
/// Defines the OpenTelemetry-compatible <see cref="Meter"/> and instruments published by
/// <c>RedisCacheManagerService</c>. Consuming applications can observe these instruments by
/// adding the <see cref="MeterName"/> to their OpenTelemetry metrics configuration
/// (e.g. <c>builder.Services.AddOpenTelemetry().WithMetrics(m => m.AddMeter(RedisCacheMetrics.MeterName))</c>).
/// No exporter dependency is taken here; the host application chooses how metrics are exported.
/// </summary>
public sealed class RedisCacheMetrics : IDisposable
{
    /// <summary>The name under which this library's <see cref="Meter"/> is registered.</summary>
    public const string MeterName = "RedisCacheManager";

    private readonly Meter _meter;

    public RedisCacheMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        CacheHits = _meter.CreateCounter<long>(
            "redis_cache.hits",
            unit: "{hit}",
            description: "Number of cache reads that found a value.");

        CacheMisses = _meter.CreateCounter<long>(
            "redis_cache.misses",
            unit: "{miss}",
            description: "Number of cache reads that did not find a value (true miss or Redis unavailable).");

        CacheSets = _meter.CreateCounter<long>(
            "redis_cache.sets",
            unit: "{set}",
            description: "Number of successful cache write operations.");

        CacheRemovals = _meter.CreateCounter<long>(
            "redis_cache.removals",
            unit: "{removal}",
            description: "Number of successful cache eviction operations.");

        CacheErrors = _meter.CreateCounter<long>(
            "redis_cache.errors",
            unit: "{error}",
            description: "Number of cache operations that failed due to a Redis exception, tagged by operation.");

        OperationDuration = _meter.CreateHistogram<double>(
            "redis_cache.operation.duration",
            unit: "ms",
            description: "Duration of cache get/set/remove operations, tagged by operation and outcome.");
    }

    /// <summary>Incremented on every cache read that resolves a value.</summary>
    public Counter<long> CacheHits { get; }

    /// <summary>Incremented on every cache read that resolves no value, including read failures.</summary>
    public Counter<long> CacheMisses { get; }

    /// <summary>Incremented on every successful cache write.</summary>
    public Counter<long> CacheSets { get; }

    /// <summary>Incremented on every successful cache eviction.</summary>
    public Counter<long> CacheRemovals { get; }

    /// <summary>Incremented on every operation that failed due to a Redis exception.</summary>
    public Counter<long> CacheErrors { get; }

    /// <summary>Records the duration, in milliseconds, of a cache operation.</summary>
    public Histogram<double> OperationDuration { get; }

    public void Dispose() => _meter.Dispose();
}
