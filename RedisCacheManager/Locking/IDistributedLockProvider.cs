namespace RedisCacheManager.Locking;

/// <summary>A handle to an acquired distributed lock. Dispose (or call ReleaseAsync) to release it.</summary>
public interface IDistributedLockHandle : IAsyncDisposable
{
    /// <summary>Whether the lock was actually acquired.</summary>
    bool Acquired { get; }

    /// <summary>Releases the lock if this handle still owns it. Safe to call multiple times.</summary>
    Task ReleaseAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides simple Redis-based distributed locking (SET NX PX acquire, token-checked Lua
/// release) so that only one process/instance performs a given operation at a time across
/// all consuming applications.
/// </summary>
public interface IDistributedLockProvider
{
    /// <summary>
    /// Attempts to acquire a named lock for up to <paramref name="expiry"/>. Returns a handle
    /// whose <see cref="IDistributedLockHandle.Acquired"/> flag indicates success; callers
    /// must check this flag before proceeding with the protected operation.
    /// </summary>
    Task<IDistributedLockHandle> AcquireAsync(string lockName, TimeSpan expiry, CancellationToken cancellationToken = default);
}

