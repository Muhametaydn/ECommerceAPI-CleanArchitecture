using ECommerce.Application.Common.Behaviors;
using ECommerce.Application.Common.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ECommerce.UnitTests.Behaviors;

// ── Test tipleri — public olmalı (Moq/Castle DynamicProxy internal tiplere erişemez) ──

public record CacheableBehaviorQuery(string Key, TimeSpan? Duration = null)
    : IRequest<string>, ICacheableRequest
{
    public string CacheKey => Key;
    public TimeSpan? CacheDuration => Duration;
}

public record NonCacheableBehaviorQuery : IRequest<string>;

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// CachingBehavior testleri.
///
/// ILogger mock'lanmaz — NullLogger&lt;T&gt;.Instance kullanılır.
/// Moq/Castle DynamicProxy, internal veya nested tipler içeren generic ILogger proxy'si üretemez.
/// NullLogger bu sorunu tamamen ortadan kaldırır.
/// </summary>
public class CachingBehaviorTests
{
    private readonly Mock<ICacheService> _cacheMock;
    private readonly CachingBehavior<CacheableBehaviorQuery, string> _behavior;
    private readonly CachingBehavior<NonCacheableBehaviorQuery, string> _ncBehavior;

    public CachingBehaviorTests()
    {
        _cacheMock = new Mock<ICacheService>();

        _behavior = new CachingBehavior<CacheableBehaviorQuery, string>(
            _cacheMock.Object,
            NullLogger<CachingBehavior<CacheableBehaviorQuery, string>>.Instance);

        _ncBehavior = new CachingBehavior<NonCacheableBehaviorQuery, string>(
            _cacheMock.Object,
            NullLogger<CachingBehavior<NonCacheableBehaviorQuery, string>>.Instance);
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────

    private static RequestHandlerDelegate<string> MakeNext(string returnValue, Action? onCall = null) =>
        ct => { onCall?.Invoke(); return Task.FromResult(returnValue); };

    private static RequestHandlerDelegate<string> MakeNullNext() =>
        ct => Task.FromResult<string>(null!);

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
        RequestHandlerDelegate<string> next = ct =>
        {
            handlerCalled = true;
            return Task.FromResult("fresh-response");
        };

        // Act
        var result = await _behavior.Handle(new CacheableBehaviorQuery(cacheKey), next, CancellationToken.None);

        // Assert
        result.Should().Be(cachedValue);
        handlerCalled.Should().BeFalse("cache HIT'te handler çağrılmamalı");
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<string>(),
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

        // Act
        var result = await _behavior.Handle(
            new CacheableBehaviorQuery(cacheKey),
            MakeNext(freshValue),
            CancellationToken.None);

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

        // Act
        await _behavior.Handle(
            new CacheableBehaviorQuery(cacheKey, customDuration),
            MakeNext("value"),
            CancellationToken.None);

        // Assert
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

        TimeSpan? capturedDuration = null;
        _cacheMock
            .Setup(c => c.SetAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TimeSpan?, CancellationToken>(
                (_, _, d, _) => capturedDuration = d)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Handle(
            new CacheableBehaviorQuery(cacheKey, Duration: null),
            MakeNext("value"),
            CancellationToken.None);

        // Assert
        capturedDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    // ── NON-CACHEABLE REQUEST BYPASS ─────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRequestIsNotCacheable_ShouldBypassCacheCompletely()
    {
        // Arrange
        var handlerCalled = false;
        RequestHandlerDelegate<string> next = ct =>
        {
            handlerCalled = true;
            return Task.FromResult("direct");
        };

        // Act
        var result = await _ncBehavior.Handle(new NonCacheableBehaviorQuery(), next, CancellationToken.None);

        // Assert
        result.Should().Be("direct");
        handlerCalled.Should().BeTrue();
        _cacheMock.Verify(
            c => c.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

        // Act
        var result = await _behavior.Handle(
            new CacheableBehaviorQuery(cacheKey),
            MakeNullNext(),
            CancellationToken.None);

        // Assert
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

        // Act
        await _behavior.Handle(
            new CacheableBehaviorQuery(cacheKey),
            MakeNext("data"),
            CancellationToken.None);

        // Assert
        _cacheMock.Verify(
            c => c.GetAsync<string>(cacheKey, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheMock.Verify(
            c => c.SetAsync(cacheKey, It.IsAny<string>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
