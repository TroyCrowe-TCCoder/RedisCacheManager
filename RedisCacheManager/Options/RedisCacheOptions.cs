namespace RedisCacheManager.Options;

/// <summary>
/// Configuration options for the shared Redis cache manager. Bound from configuration
/// (e.g. appsettings.json section "RedisCacheManager") and combined with a secret
/// resolved from Azure Key Vault at startup for the access key value.
/// </summary>
public sealed class RedisCacheOptions
{
    /// <summary>Configuration section name used when binding these options.</summary>
    public const string SectionName = "RedisCacheManager";

    /// <summary>The Redis host name (without port), e.g. "myapp-cache.redis.cache.windows.net".</summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>The Redis port. Defaults to the standard TLS port for Azure Cache for Redis.</summary>
    public int Port { get; set; } = 6380;

    /// <summary>Name of the Key Vault secret that holds the Redis access key. The key value is never stored in configuration.</summary>
    public string AccessKeySecretName { get; set; } = string.Empty;

    /// <summary>Key Vault URI (e.g. "https://myvault.vault.azure.net/") used to resolve <see cref="AccessKeySecretName"/>.</summary>
    public string KeyVaultUri { get; set; } = string.Empty;

    /// <summary>Logical instance/application name used as the first segment of every cache key (e.g. "CaptiveIdentity").</summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>Default time-to-live applied to cache entries when the caller does not specify one.</summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Whether TLS/SSL is required for the Redis connection. Must remain true for Azure Cache for Redis.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Maximum number of retry attempts for transient Redis failures before treating the operation as a cache-miss.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Base delay used for exponential backoff between retry attempts.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Connection and synchronous operation timeout applied to the underlying Redis connection.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

