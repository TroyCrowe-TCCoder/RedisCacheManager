using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedisCacheManager.Caching;
using RedisCacheManager.Extensions;
using RedisCacheManager.Options;
using Xunit;

namespace RedisCacheManager.Tests;

public class ServiceCollectionExtensionsTests
{
    private static void Valid(RedisCacheOptions o)
    {
        o.HostName = "cache.example.net";
        o.Port = 6380;
        o.InstanceName = "App";
        o.DefaultTtl = TimeSpan.FromMinutes(10);
        o.UseSsl = true;
        o.MaxRetryAttempts = 2;
        o.RetryBaseDelay = TimeSpan.FromMilliseconds(50);
        o.ConnectTimeout = TimeSpan.FromSeconds(3);
    }

    private static ServiceProvider Build(Action<RedisCacheOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRedisCacheManager(configure);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void WhenAllValuesSuppliedOptionsCarryTheHostValues()
    {
        using var provider = Build(Valid);

        var options = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;

        Assert.Equal("cache.example.net", options.HostName);
        Assert.Equal(6380, options.Port);
        Assert.Equal(TimeSpan.FromMinutes(10), options.DefaultTtl);
        Assert.True(options.UseSsl);
    }

    [Fact]
    public void WhenAllValuesSuppliedKeyBuilderResolves()
    {
        using var provider = Build(Valid);

        Assert.NotNull(provider.GetRequiredService<IRedisKeyBuilder>());
    }

    [Fact]
    public void LibraryDefinesNoDefaults()
    {
        var options = new RedisCacheOptions();

        Assert.Equal(0, options.Port);
        Assert.Equal(TimeSpan.Zero, options.DefaultTtl);
        Assert.Equal(TimeSpan.Zero, options.ConnectTimeout);
        Assert.Equal(TimeSpan.Zero, options.RetryBaseDelay);
        Assert.Equal(0, options.MaxRetryAttempts);
        Assert.False(options.UseSsl);
    }

    [Fact]
    public void WhenNothingIsSuppliedOptionsValidationFails()
    {
        using var provider = Build(_ => { });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value);
    }

    [Theory]
    [InlineData("HostName")]
    [InlineData("Port")]
    [InlineData("InstanceName")]
    [InlineData("DefaultTtl")]
    [InlineData("ConnectTimeout")]
    [InlineData("MaxRetryAttempts")]
    [InlineData("RetryBaseDelay")]
    [InlineData("KeyVaultWithoutSecretName")]
    public void WhenAnInvalidValueIsSuppliedOptionsValidationFails(string invalid)
    {
        using var provider = Build(o =>
        {
            Valid(o);
            switch (invalid)
            {
                case "HostName": o.HostName = " "; break;
                case "Port": o.Port = 0; break;
                case "InstanceName": o.InstanceName = string.Empty; break;
                case "DefaultTtl": o.DefaultTtl = TimeSpan.Zero; break;
                case "ConnectTimeout": o.ConnectTimeout = TimeSpan.Zero; break;
                case "MaxRetryAttempts": o.MaxRetryAttempts = -1; break;
                case "RetryBaseDelay": o.RetryBaseDelay = TimeSpan.FromMilliseconds(-1); break;
                case "KeyVaultWithoutSecretName": o.KeyVaultUri = "https://vault.vault.azure.net/"; break;
            }
        });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value);
    }

    [Fact]
    public void WhenKeyVaultIsNotUsedNoSecretNameIsRequired()
    {
        using var provider = Build(Valid);

        var options = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;

        Assert.Equal(string.Empty, options.KeyVaultUri);
    }

    [Fact]
    public void WhenConfigureIsNullAddRedisCacheManagerThrows()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddRedisCacheManager(null!));
    }
}
