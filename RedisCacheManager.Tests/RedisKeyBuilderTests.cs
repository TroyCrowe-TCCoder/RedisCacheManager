using Microsoft.Extensions.Options;
using RedisCacheManager.Caching;
using RedisCacheManager.Options;
using Xunit;

namespace RedisCacheManager.Tests;

public class RedisKeyBuilderTests
{
    [Fact]
    public void Build_ProducesInstanceCategoryIdFormat()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RedisCacheOptions { InstanceName = "CaptiveIdentity" });
        var builder = new RedisKeyBuilder(options);

        var key = builder.Build("EffectiveRoles", "user-123");

        Assert.Equal("CaptiveIdentity:EffectiveRoles:user-123", key);
    }

    [Fact]
    public void Build_ThrowsOnEmptyCategory()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RedisCacheOptions { InstanceName = "CaptiveIdentity" });
        var builder = new RedisKeyBuilder(options);

        Assert.Throws<ArgumentException>(() => builder.Build(string.Empty, "user-123"));
    }
}

