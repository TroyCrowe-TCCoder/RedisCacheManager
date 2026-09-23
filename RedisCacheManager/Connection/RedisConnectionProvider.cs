using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedisCacheManager.Options;
using StackExchange.Redis;

namespace RedisCacheManager.Connection;

/// <summary>
/// Resolves the Redis access key from Azure Key Vault and produces a single shared
/// <see cref="IConnectionMultiplexer"/> for the application. The multiplexer is created
/// lazily and reused for the lifetime of the process; StackExchange.Redis handles its own
/// internal reconnect logic once connected.
/// </summary>
public sealed class RedisConnectionProvider : IRedisConnectionProvider, IAsyncDisposable
{
    private readonly RedisCacheOptions _options;
    private readonly ILogger<RedisConnectionProvider> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IConnectionMultiplexer? _multiplexer;

    public RedisConnectionProvider(IOptions<RedisCacheOptions> options, ILogger<RedisConnectionProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IConnectionMultiplexer> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_multiplexer is { IsConnected: true })
        {
            return _multiplexer;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_multiplexer is { IsConnected: true })
            {
                return _multiplexer;
            }

            var accessKey = await ResolveAccessKeyAsync(cancellationToken).ConfigureAwait(false);
            var configuration = new ConfigurationOptions
            {
                EndPoints = { { _options.HostName, _options.Port } },
                Password = accessKey,
                Ssl = _options.UseSsl,
                AbortOnConnectFail = false,
                ConnectTimeout = (int)_options.ConnectTimeout.TotalMilliseconds,
                SyncTimeout = (int)_options.ConnectTimeout.TotalMilliseconds,
            };

            _multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration).ConfigureAwait(false);
            return _multiplexer;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<string> ResolveAccessKeyAsync(CancellationToken cancellationToken)
    {
        var client = new SecretClient(new Uri(_options.KeyVaultUri), new DefaultAzureCredential());
        KeyVaultSecret secret = await client.GetSecretAsync(_options.AccessKeySecretName, cancellationToken: cancellationToken).ConfigureAwait(false);
        return secret.Value;
    }

    public async ValueTask DisposeAsync()
    {
        if (_multiplexer is not null)
        {
            await _multiplexer.CloseAsync().ConfigureAwait(false);
            _multiplexer.Dispose();
        }

        _initLock.Dispose();
    }
}

