# RedisCacheManager

## Product
Shared Redis caching, distributed locking, health check, and resilience library consumed by CaptiveIdentityAPI, CaptiveExpensesAPI, and future APIs. Provides typed get/set/remove cache operations with cache-miss-on-failure semantics, a conventioned key builder to prevent cross-app key collisions, Polly-based retry for transient Redis outages, and a Redis-based distributed lock helper.

## Planning
The access key for Azure Cache for Redis is resolved from Azure Key Vault at startup rather than stored in configuration. All cache read/write/remove failures (true cache-miss or Redis unavailability) are surfaced to callers as a simple "not found" result and logged internally; the library never throws for these conditions, so callers are expected to fall back to their own source of truth (e.g. a database call).

