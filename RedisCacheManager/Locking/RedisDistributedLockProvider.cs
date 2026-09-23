using Microsoft.Extensions.Logging;
using RedisCacheManager.Connection;
using StackExchange.Redis;

namespace RedisCacheManager.Locking;

/// <summary>Default Redis-based implementation of <see cref="IDistributedLockProvider"/>.</summary>
public sealed class RedisDistributedLockProvider : IDistributedLockProvider
{
    private const string ReleaseScript = @"if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private readonly IRedisConnectionProvider _connectionProvider;
    private readonly ILogger<RedisDistributedLockProvider> _logger;

    public RedisDistributedLockProvider(IRedisConnectionProvider connectionProvider, ILogger<RedisDistributedLockProvider> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public async Task<IDistributedLockHandle> AcquireAsync(string lockName, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var token = Guid.NewGuid().ToString("N");
        try
        {
            var connection = await _connectionProvider.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            var database = connection.GetDatabase();
            var acquired = await database.StringSetAsync(lockName, token, expiry, When.NotExists).ConfigureAwait(false);
            return new RedisDistributedLockHandle(database, lockName, token, acquired, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to acquire distributed lock {LockName}; treating as not acquired.", lockName);
            return new RedisDistributedLockHandle(null, lockName, token, acquired: false, _logger);
        }
    }

    private sealed class RedisDistributedLockHandle : IDistributedLockHandle
    {
        private readonly IDatabase? _database;
        private readonly string _lockName;
        private readonly string _token;
        private readonly ILogger _logger;
        private bool _released;

        public RedisDistributedLockHandle(IDatabase? database, string lockName, string token, bool acquired, ILogger logger)
        {
            _database = database;
            _lockName = lockName;
            _token = token;
            Acquired = acquired;
            _logger = logger;
        }

        public bool Acquired { get; }

        public async Task ReleaseAsync(CancellationToken cancellationToken = default)
        {
            if (_released || !Acquired || _database is null)
            {
                return;
            }

            try
            {
                await _database.ScriptEvaluateAsync(ReleaseScript, new RedisKey[] { _lockName }, new RedisValue[] { _token }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release distributed lock {LockName}.", _lockName);
            }
            finally
            {
                _released = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await ReleaseAsync().ConfigureAwait(false);
        }
    }
}

