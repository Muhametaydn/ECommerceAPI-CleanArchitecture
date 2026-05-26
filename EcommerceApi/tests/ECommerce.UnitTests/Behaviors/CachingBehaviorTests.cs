using ECommerce.Application.Common.Behaviors;
using ECommerce.Application.Common.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.UnitTests.Behaviors;

/// <summary>
/// CachingBehavior&lt;TRequest, TResponse&gt; testleri.
///
/// Sorumluluk:
/// 1. ICacheableRequest olan query'lerde cache'i kontrol eder.
/// 2. Cache HIT → handler çağrılmaz, cache'den döner.
/// 3. Cache MISS → handler çağrılır, sonuç cache'e yazılır.
/// 4. ICacheableRequest olmayan request'ler bypass edilir.
/// 5. CacheDuration null ise varsayılan 5 dakika kullanılır.
/// </summary>
public class CachingBehaviorTests
{
    // ── Test double'lar ───────────────────────────────────────────────────

    /// ICacheableRequest implement eden örnek query
    private sealed record CacheableQuery(string Key, TimeSpan? Duration = null)
        : IRequest<string>, ICacheableRequest
    {
        public string CacheKey => Key;
        public TimeSpan? CacheDuration => Duration;
    }

    /// ICacheableRequest implement etmeyen sıradan query
    private sealed record NonCacheableQuery : IRequest<string>;

    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<CachingBehavior<CacheableQuery, string>>> _loggerMock;
    private readonly CachingBehavior<CacheableQuery, string> _behavior;

    private readonly Mock<ILogger<CachingBehavior<NonCacheableQuery, string>>> _ncLoggerMock;
    private readonly CachingBehavior<NonCacheableQuery, string> _ncBehavior;

    public CachingBehaviorTests()
    {
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<CachingBehavior<CacheableQuery, string>>>();
        _behavior = new CachingBehavior<CacheableQuery, string>(_cacheMock.Object, _loggerMock.Object);

        _ncLoggerMock = new Mock<ILogger<CachingBehavior<NonCacheableQuery, string>>>();
        _ncBehavior = new CachingBehavior<NonCacheableQuery, string>(_cacheMock.Object, _ncLoggerMock.Object);
    }

    // ── CACHE HIT ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedValueWithoutCallingHandler()
    {
        // Arrange
        const string cacheKey = "test-key";
        const string cachedValue = "cached-response";

        _cacheMock
            .Setup(c => c.GetAsync<string>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedValue);

        var handlerCalled = false;
        Task<string> Next() { handlerCalled = true; return Task.FromResult("fresh-response"); }

        var query = new CacheableQuery(cacheKey);

        // Act
        var result = await _behavior.Handle(query, Next, CancellationToken.None);

        // Assert
        result.Should().Be(cachedValue);
        handlerCalled.Should().BeFalse("cache HIT'te handler çağrılmamalı");
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── CACHE MISS ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldCallHandlerAndStoreResult()
    {
        // Arrange
        const string cacheKey = "miss-key";
        const string freshValue = "fresh-from-db";

        _cacheMock
            .Setup(c => c.GetAsync<string>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var query = new CacheableQuery(cacheKey);

        // Act
        var result = await _behavior.Handle(query, () => Task.FromResult(freshValue), CancellationToken.None);

        // Assert
        result.Should().Be(freshValue);
        _cacheMock.Verify(
            c => c.SetAsync(cacheKey, freshValue, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldUseSpecifiedCacheDuration()
    {
        // Arrange
        var customDuration = TimeSpan.FromMinutes(30);
        const string cacheKey = "duration-key";

        _cacheMock
            .Setup(c => c.GetAsync<string>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var query = new CacheableQuery(cacheKey, customDuration);

        // Act
        await _behavior.Handle(query, () => Task.FromResult("value"), CancellationToken.None);

        // Assert — belirtilen süre SetAsync'e iletilmeli
        _cacheMock.Verify(
            c => c.SetAsync(cacheKey, "value", customDuration, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheDurationIsNull_ShouldUseDefaultFiveMinutes()
    {
        // Arrange
        const string cacheKey = "default-duration-key";

        _cacheMock
            .Setup(c => c.GetAsync<string>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // CacheDuration = null → varsayılan 5 dakika uygulanmalı
        var query = new CacheableQuery(cacheKey, Duration: null);

        TimeSpan? capturedDuration = null;
        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TimeSpan?, CancellationToken>(
                (_, _, d, _) => capturedDuration = d);

        // Act
        await _behavior.Handle(query, () => Task.FromResult("value"), CancellationToken.None);

        // Assert
        capturedDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    // ── NON-CACHEABLE REQUEST BYPASS ─────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRequestIsNotCacheable_ShouldBypassCacheCompletely()
    {
        // Arrange
        var query = new NonCacheableQuery();
        var handlerCalled = false;

        // Act
        var result = await _ncBehavior.Handle(
            query,
            () => { handlerCalled = true; return Task.FromResult("direct"); },
            CancellationToken.None);

        // Assert
        result.Should().Be("direct");
        handlerCalled.Should().BeTrue();
        _cacheMock.Verify(c => c.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── NULL HANDLER RESPONSE ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenHandlerReturnsNull_ShouldNotWriteToCache()
    {
        // Arrange
        const string cacheKey = "null-response-key";

        _cacheMock
            .Setup(c => c.GetAsync<string>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var query = new CacheableQuery(cacheKey);

        // Act
        var result = await _behavior.Handle(query, () => Task.FromResult<string>(null!), CancellationToken.None);

        // Assert — null sonuç cache'e yazılmamalı
        result.Should().BeNull();
        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── CACHE KEY DOĞRULUĞU ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReadAndWriteWithSameCacheKey()
    {
        // Arrange
        const string cacheKey = "my:specific:cache:key";

        _cacheMock
            .Setup(c => c.GetAsync<string>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var query = new CacheableQuery(cacheKey);

        // Act
        await _behavior.Handle(query, () => Task.FromResult("data"), CancellationToken.None);

        // Assert — GetAsync ve SetAsync aynı key ile çağrılmalı
        _cacheMock.Verify(c => c.GetAsync<string>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(cacheKey, It.IsAny<string>(),
            It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
