# RedisCacheManager configuration reference

Every option of this package, its default, and whether it is required or secret. The host app supplies all values
in code through `AddRedisCacheManager`. The package reads no configuration file and no environment variable, so
where the host keeps the values is the host's choice. A test keeps this file in step with the code, so a default
listed here is the default in the package.

| Column | Meaning |
|---|---|
| Default | The value used when the host sets nothing. `none` means the option has no value until the host sets one. |
| Required | `Yes` means the host must supply a value. `Conditional` means it is required under a stated condition. `No` means it can be left alone. A required option is checked when the host starts, and a missing or invalid value stops the host with a message that names the option. |
| Secret | `Yes` means the value is a secret. It comes from Key Vault when the app is deployed and from user secrets in local development. It never goes in a configuration file. |

## Supplying the access key

The Redis access key is a secret and is never an option. The host sets `KeyVaultUri` and `AccessKeySecretName`,
and the package reads the key from that Key Vault secret at startup. `AccessKeySecretName` is the name of the
secret, not its value, so it is not itself a secret. When `KeyVaultUri` is empty, no access key is used.

```csharp
builder.Services.AddRedisCacheManager(options =>
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

## `RedisCacheOptions`

Most options have no default and must be supplied. `UseSsl`, `MaxRetryAttempts` and `RetryBaseDelay` can be left
alone because `false` and zero are valid values for them.

| Option | Default | Required | Secret | Meaning |
|---|---|---|---|---|
| `HostName` | `none` | Yes | No | The Redis host name without the port, for example `myapp-cache.redis.cache.windows.net` |
| `Port` | `none` | Yes | No | The Redis port. Must be above 0. Azure Cache for Redis uses 6380 for TLS |
| `InstanceName` | `none` | Yes | No | The application name used as the first segment of every cache key, for example `captive-identity` |
| `KeyVaultUri` | `none` | No | No | The Key Vault that holds the access key secret, for example `https://myvault.vault.azure.net/`. When empty, no access key is used |
| `AccessKeySecretName` | `none` | Conditional | No | The name of the Key Vault secret that holds the Redis access key. Required when `KeyVaultUri` is set |
| `DefaultTtl` | `none` | Yes | No | The time to live applied to a cache entry when the caller does not give one. Must be above 0 |
| `UseSsl` | `false` | No | No | Whether the connection uses TLS. Set it to `true` for Azure Cache for Redis and for any shared or cloud Redis. It is not validated, so a local Redis without TLS can be used |
| `MaxRetryAttempts` | `0` | No | No | How many times a transient Redis failure is retried before the call is treated as a cache miss. 0 means no retry. Must not be negative |
| `RetryBaseDelay` | `0 s` | No | No | The base delay of the exponential backoff between retries. Must not be negative |
| `ConnectTimeout` | `none` | Yes | No | The connection and synchronous operation timeout. Must be above 0 |
