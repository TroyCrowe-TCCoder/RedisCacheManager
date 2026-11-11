# RedisCacheManager.CacheConsumer

## Product
Read-only cache abstraction layered on top of `TCCoder.RedisCacheManager`'s `ICacheManager`. Exposes only a `TryGetAsync` read path, with no `SetAsync`/`RemoveAsync` members, so a consumer API cannot accidentally write to or evict entries in a cache it does not own. Intended for services that read data populated and evicted exclusively by an upstream owner (e.g. `CaptiveIdentityAPI` owning effective-role cache entries that `CaptiveExpensesAPI`, `DocumentManagerAPI`, and other downstream APIs only ever read).

## Planning
`AddCacheConsumer` assumes `ICacheManager` is already registered (typically via `TCCoder.RedisCacheManager`'s `AddRedisCacheManager(configure)`, where the host supplies every option value in code) and simply layers the read-only wrapper on top.
