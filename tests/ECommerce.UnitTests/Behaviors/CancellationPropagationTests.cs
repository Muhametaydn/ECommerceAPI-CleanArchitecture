using ECommerce.Application.Common.Behaviors;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ECommerce.UnitTests.Behaviors;

public sealed class CancellationPropagationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Caching_ForwardsCancellationToHandler(bool cacheable)
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var cache = Mock.Of<ICacheService>();
        Func<Task> action = cacheable
            ? () => new CachingBehavior<CacheableBehaviorQuery, string>(cache,
                    NullLogger<CachingBehavior<CacheableBehaviorQuery, string>>.Instance)
                .Handle(new CacheableBehaviorQuery("cancellation-test"), ct => Task.FromCanceled<string>(ct), source.Token)
            : () => new CachingBehavior<NonCacheableBehaviorQuery, string>(cache,
                    NullLogger<CachingBehavior<NonCacheableBehaviorQuery, string>>.Instance)
                .Handle(new NonCacheableBehaviorQuery(), ct => Task.FromCanceled<string>(ct), source.Token);

        var failure = await action.Should().ThrowAsync<OperationCanceledException>();
        failure.Which.CancellationToken.Should().Be(source.Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Validation_ForwardsCancellationToHandler(bool hasValidator)
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var validator = new InlineValidator<NonCacheableBehaviorQuery>();
        var behavior = new ValidationBehavior<NonCacheableBehaviorQuery, string>(
            hasValidator ? new[] { validator } : Array.Empty<IValidator<NonCacheableBehaviorQuery>>());

        Func<Task> action = () => behavior.Handle(new NonCacheableBehaviorQuery(),
            ct => Task.FromCanceled<string>(ct), source.Token);

        var failure = await action.Should().ThrowAsync<OperationCanceledException>();
        failure.Which.CancellationToken.Should().Be(source.Token);
    }

    [Fact]
    public async Task StockInvalidation_ForwardsCancellationBeforeCommit()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        var behavior = new StockCacheInvalidationBehavior<NonCacheableBehaviorQuery, string>(
            unitOfWork.Object, cache.Object,
            NullLogger<StockCacheInvalidationBehavior<NonCacheableBehaviorQuery, string>>.Instance);

        Func<Task> action = () => behavior.Handle(new NonCacheableBehaviorQuery(),
            ct => Task.FromCanceled<string>(ct), source.Token);

        var failure = await action.Should().ThrowAsync<OperationCanceledException>();
        failure.Which.CancellationToken.Should().Be(source.Token);
        unitOfWork.VerifyNoOtherCalls();
        cache.VerifyNoOtherCalls();
    }
}
