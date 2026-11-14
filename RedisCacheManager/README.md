# RedisCacheManager

## Product
Shared Redis caching, distributed locking, health check, and resilience library consumed by CaptiveIdentityAPI, CaptiveExpensesAPI, and future APIs. Provides typed get/set/remove cache operations with cache-miss-on-failure semantics, a conventioned key builder to prevent cross-app key collisions, Polly-based retry for transient Redis outages, and a Redis-based distributed lock helper.

## Planning
The access key for Azure Cache for Redis is resolved from Azure Key Vault at startup rather than stored in configuration. All cache read/write/remove failures (true cache-miss or Redis unavailability) are surfaced to callers as a simple "not found" result and logged internally; the library never throws for these conditions, so callers are expected to fall back to their own source of truth (e.g. a database call).

## Configuration
The library follows the two-part configuration model (ADR-0002). It has no defaults, reads no `IConfiguration`, environment variables, or files, and has no section name. The host reads its own configuration and passes every value in code:

```csharp
services.AddRedisCacheManager(options =>
{
    options.HostName = settings.Redis.HostName;
    options.Port = 6380;
    options.UseSsl = true;
    options.InstanceName = "captive-identity";
    options.KeyVaultUri = settings.KeyVault.Uri;
    options.AccessKeySecretName = "redis-access-key";
    options.DefaultTtl = TimeSpan.FromMinutes(15);
    options.MaxRetryAttempts = 3;
    options.RetryBaseDelay = TimeSpan.FromMilliseconds(100);
    options.ConnectTimeout = TimeSpan.FromSeconds(5);
});
```

Options are validated on start and the host fails to start if any rule is violated:
- `HostName` and `InstanceName` are required.
- `Port` and `ConnectTimeout` must be greater than zero, and so must `DefaultTtl`.
- `MaxRetryAttempts` and `RetryBaseDelay` must not be negative.
- `AccessKeySecretName` is required when `KeyVaultUri` is set. `KeyVaultUri` is optional.
- `UseSsl` must be `true` for Azure Cache for Redis. It is not validated so a local development Redis without TLS can be used.

