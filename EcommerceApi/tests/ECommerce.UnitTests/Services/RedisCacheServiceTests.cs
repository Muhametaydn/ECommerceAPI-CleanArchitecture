using ECommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace ECommerce.UnitTests.Services;

/// <summary>
/// RedisCacheService testleri.
///
/// IDistributedCache ve IConnectionMultiplexer mock'lanır.
/// Hata senaryolarında exception yutulduğu doğrulanır (servis asla fırlatmamalı).
/// </summary>
public class RedisCacheServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IConnectionMultiplexer> _multiplexerMock;
    private readonly Mock<IServer> _serverMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock;
    private readonly RedisCacheService _sut;

    public RedisCacheServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _multiplexerMock = new Mock<IConnectionMultiplexer>();
        _serverMock = new Mock<IServer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<RedisCacheService>>();

        // GetEndPoints + GetServer + GetDatabase zinciri
        _multiplexerMock
            .Setup(m => m.GetEndPoints(It.IsAny<bool>()))
            .Returns(new EndPoint[] { new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6379) });
        _multiplexerMock
            .Setup(m => m.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>()))
            .Returns(_serverMock.Object);
        _multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _sut = new RedisCacheService(_cacheMock.Object, _multiplexerMock.Object, _loggerMock.Object);
    }

    // ── GetAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnDeserializedValue()
    {
        // Arrange
        const string key = "test:key";
        var expected = new TestData { Id = 42, Name = "Ürün" };
        var json = JsonSerializer.Serialize(expected, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _cacheMock
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        // Act
        var result = await _sut.GetAsync<TestData>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("Ürün");
    }

    [Fact]
    public async Task GetAsync_WhenKeyMissing_ShouldReturnDefault()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.GetAsync<TestData>("missing:key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenRedisThrows_ShouldReturnDefaultWithoutThrowing()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "bağlantı yok"));

        // Act
        var act = async () => await _sut.GetAsync<TestData>("error:key");

        // Assert — exception dışarı fırlatılmamalı
        await act.Should().NotThrowAsync();
        var result = await _sut.GetAsync<TestData>("error:key");
        result.Should().BeNull();
    }

    // ── SetAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SetAsync_ShouldSerializeAndStoreValue()
    {
        // Arrange
        const string key = "products:list:1";
        var value = new TestData { Id = 1, Name = "Laptop" };
        string? capturedJson = null;

        _cacheMock
            .Setup(c => c.SetStringAsync(key, It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, DistributedCacheEntryOptions, CancellationToken>(
                (_, json, _, _) => capturedJson = json);

        // Act
        await _sut.SetAsync(key, value, TimeSpan.FromMinutes(10));

        // Assert
        capturedJson.Should().NotBeNullOrEmpty();
        var deserialized = JsonSerializer.Deserialize<TestData>(capturedJson!,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        deserialized!.Id.Should().Be(1);
        deserialized.Name.Should().Be("Laptop");
    }

    [Fact]
    public async Task SetAsync_WhenExpiryIsNull_ShouldUseDefaultFiveMinuteTtl()
    {
        // Arrange
        DistributedCacheEntryOptions? capturedOptions = null;

        _cacheMock
            .Setup(c => c.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, DistributedCacheEntryOptions, CancellationToken>(
                (_, _, opts, _) => capturedOptions = opts);

        // Act
        await _sut.SetAsync("key", "value", expiry: null);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task SetAsync_WhenExpiryProvided_ShouldUseThatTtl()
    {
        // Arrange
        var customTtl = TimeSpan.FromHours(1);
        DistributedCacheEntryOptions? capturedOptions = null;

        _cacheMock
            .Setup(c => c.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, DistributedCacheEntryOptions, CancellationToken>(
                (_, _, opts, _) => capturedOptions = opts);

        // Act
        await _sut.SetAsync("key", "value", expiry: customTtl);

        // Assert
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(customTtl);
    }

    [Fact]
    public async Task SetAsync_WhenRedisThrows_ShouldNotThrow()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RedisTimeoutException("timeout", CommandStatus.Unknown));

        // Act
        var act = async () => await _sut.SetAsync("key", "value");

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_ShouldCallDistributedCacheRemove()
    {
        // Arrange
        const string key = "products:single:abc";

        // Act
        await _sut.RemoveAsync(key);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenRedisThrows_ShouldNotThrow()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "bağlantı yok"));

        // Act
        var act = async () => await _sut.RemoveAsync("key");

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── RemoveByPrefixAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RemoveByPrefixAsync_WhenKeysFound_ShouldDeleteAllMatchingKeys()
    {
        // Arrange
        const string prefix = "products:list:";
        var matchingKeys = new RedisKey[]
        {
            new("ECommerce:products:list:1"),
            new("ECommerce:products:list:2"),
            new("ECommerce:products:list:3")
        };

        _serverMock
            .Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(),
                It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(matchingKeys.AsEnumerable());

        _databaseMock
            .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(3);

        // Act
        await _sut.RemoveByPrefixAsync(prefix);

        // Assert
        _databaseMock.Verify(
            d => d.KeyDeleteAsync(It.Is<RedisKey[]>(k => k.Length == 3), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveByPrefixAsync_WhenNoKeysFound_ShouldNotCallDelete()
    {
        // Arrange
        _serverMock
            .Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(),
                It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(Enumerable.Empty<RedisKey>());

        // Act
        await _sut.RemoveByPrefixAsync("empty:prefix:");

        // Assert
        _databaseMock.Verify(
            d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveByPrefixAsync_WhenRedisThrows_ShouldNotThrow()
    {
        // Arrange
        _serverMock
            .Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(),
                It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "bağlantı yok"));

        // Act
        var act = async () => await _sut.RemoveByPrefixAsync("prefix:");

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private sealed class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
