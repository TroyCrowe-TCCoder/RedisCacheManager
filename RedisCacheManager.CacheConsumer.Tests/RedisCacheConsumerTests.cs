using Moq;
using RedisCacheManager.Caching;
using Xunit;

namespace RedisCacheManager.CacheConsumer.Tests;

public class RedisCacheConsumerTests
{
    [Fact]
    public async Task TryGetAsync_DelegatesToUnderlyingCacheManager()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager
            .Setup(m => m.TryGetAsync<string>("category", "id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "value"));

        var consumer = new RedisCacheConsumer(mockCacheManager.Object);

        var (found, value) = await consumer.TryGetAsync<string>("category", "id");

        Assert.True(found);
        Assert.Equal("value", value);
        mockCacheManager.Verify(m => m.TryGetAsync<string>("category", "id", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_ThrowsOnNullCacheManager()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisCacheConsumer(null!));
    }
}
