using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ECommerce.IntegrationTests;

public sealed class RedisFactAttribute : FactAttribute
{
    public RedisFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ECOMMERCE_TEST_REDIS")))
            Skip = "Set ECOMMERCE_TEST_REDIS to test atomic Redis cart cleanup with isolated keys.";
    }
}

public sealed class RedisCartAtomicCleanupTests : IAsyncLifetime
{
    private readonly string _prefix = "ECommerceTest:" + Guid.NewGuid().ToString("N") + ":";
    private readonly string _key = "cart:anon:" + Guid.NewGuid().ToString("N");
    private ServiceProvider? _services;
    private IConnectionMultiplexer? _redis;
    private ICartService _cartService = null!;

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("ECOMMERCE_TEST_REDIS")!;
        _redis = await ConnectionMultiplexer.ConnectAsync(connection);
        var services = new ServiceCollection();
        services.AddSingleton(_redis);
        services.AddStackExchangeRedisCache(options => { options.Configuration = connection; options.InstanceName = _prefix; });
        services.AddSingleton<ICartService, RedisCartService>();
        _services = services.BuildServiceProvider();
        _cartService = _services.GetRequiredService<ICartService>();
    }

    public async Task DisposeAsync()
    {
        if (_redis is not null) await _redis.GetDatabase().KeyDeleteAsync(_prefix + _key);
        if (_services is not null) await _services.DisposeAsync();
        if (_redis is not null) await _redis.DisposeAsync();
    }

    private Cart Basket()
    {
        var cart = new Cart { Id = _key };
        cart.AddItem(Guid.NewGuid(), "Test", 100, 1);
        return cart;
    }

    [RedisFact]
    public async Task Unchanged_cart_is_deleted_atomically()
    {
        await _cartService.SaveCartAsync(Basket());
        var snapshot = await _cartService.GetCartAsync(_key);
        Assert.True(await _cartService.DeleteCartIfUnchangedAsync(_key, snapshot!));
        Assert.Null(await _cartService.GetCartAsync(_key));
    }

    [RedisFact]
    public async Task Changed_cart_is_preserved()
    {
        await _cartService.SaveCartAsync(Basket());
        var snapshot = await _cartService.GetCartAsync(_key);
        var newer = await _cartService.GetCartAsync(_key);
        newer!.AddItem(newer.Items[0].ProductId, "Test", 100, 2);
        await _cartService.SaveCartAsync(newer);
        Assert.False(await _cartService.DeleteCartIfUnchangedAsync(_key, snapshot!));
        Assert.Equal(3, (await _cartService.GetCartAsync(_key))!.TotalItems);
    }

    [RedisFact]
    public async Task Missing_cart_cleanup_is_safe()
        => Assert.False(await _cartService.DeleteCartIfUnchangedAsync(_key, Basket()));
}