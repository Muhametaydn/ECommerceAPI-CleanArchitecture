using ECommerce.Infrastructure.Events;
using System.Collections.Concurrent;
using ECommerce.Application;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Orders.Commands.CancelOrder;
using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Application.Features.Payments.Commands.RefundPayment;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence;
using ECommerce.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;

namespace ECommerce.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ECOMMERCE_TEST_POSTGRES")))
            Skip = "Set ECOMMERCE_TEST_POSTGRES to a PostgreSQL account with CREATE DATABASE permission. Each test creates and drops its own isolated database.";
    }
}

// Preload both contexts before executing commands so the competing requests
// deterministically observe the same old values. Writes run concurrently.
public sealed class PostgresConcurrencyTests : IAsyncLifetime
{
    private readonly string _database = "ecommerce_concurrency_" + Guid.NewGuid().ToString("N");
    private string _adminConnection = null!;
    private ServiceProvider? _services;
    private bool _databaseCreated;
    private readonly ConcurrentDictionary<string, Cart> _carts = new();
    private readonly ConcurrentDictionary<string, bool> _deleted = new();
    private Guid _customer, _address;
    private TaskCompletionSource<bool>? _cartBarrier;
    private int _cartReads;
    private readonly Guid _first = Guid.NewGuid();
    private readonly Guid _second = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        var admin = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ECOMMERCE_TEST_POSTGRES")) { Database = "postgres" };
        _adminConnection = admin.ConnectionString;
        await using (var connection = new NpgsqlConnection(_adminConnection))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", connection);
            await command.ExecuteNonQueryAsync();
            _databaseCreated = true;
        }
        var target = new NpgsqlConnectionStringBuilder(_adminConnection) { Database = _database };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddApplicationServices();
        services.AddSingleton(Mock.Of<IUserLookupService>());
        services.AddPersistenceServices(new ConfigurationBuilder().Build());
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(target.ConnectionString));
        services.AddSingleton(Mock.Of<ICacheService>());
        var cartService = new Mock<ICartService>();
        cartService.Setup(value => value.GetCartAsync(It.IsAny<string>())).Returns((string key) => ReadCart(key));
        cartService.Setup(value => value.DeleteCartIfUnchangedAsync(It.IsAny<string>(), It.IsAny<Cart>())).Callback((string key, Cart _) => _deleted.TryAdd(key, true)).ReturnsAsync(true);
        services.AddSingleton(cartService.Object);
        _services = services.BuildServiceProvider();
        await WithDb(async db =>
        {
            await db.Database.EnsureCreatedAsync();
            var customer = ApplicationUser.Create("Test", "Customer", "customer@example.invalid", "test-customer");
            _customer = customer.Id;
            db.Set<ApplicationUser>().Add(customer);
            var address = new Address { Id = Guid.NewGuid(), UserId = customer.Id, AddressLine = "Test address", City = "Istanbul", District = "Test" };
            _address = address.Id;
            db.Address.Add(address);
            var category = Guid.Parse("a1111111-1111-1111-1111-111111111111");
            db.Products.AddRange(
                new Product { Id = _first, Name = "First", SKU = "FIRST", CategoryId = category, Price = 100, StockQuantity = 1 },
                new Product { Id = _second, Name = "Second", SKU = "SECOND", CategoryId = category, Price = 100, StockQuantity = 1 });
            await db.SaveChangesAsync();
        });
        AddCart("a", _first);
        AddCart("b", _first);
    }

    public async Task DisposeAsync()
    {
        if (_services is not null) await _services.DisposeAsync();
        if (!_databaseCreated) return;
        // This generated name is the only deletion target; never use a database from configuration.
        if (!System.Text.RegularExpressions.Regex.IsMatch(_database, "^ecommerce_concurrency_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Invalid test database cleanup target.");
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(_adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE \"{_database}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<Cart?> ReadCart(string key)
    {
        var snapshot = _deleted.ContainsKey(key) ? null : _carts.GetValueOrDefault(key);
        if (_cartBarrier is not null)
        {
            if (Interlocked.Increment(ref _cartReads) == 2) _cartBarrier.TrySetResult(true);
            await _cartBarrier.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        return snapshot;
    }

    [PostgresFact]
    public async Task Concurrent_same_request_returns_one_order_to_both_callers()
    {
        _cartBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var a = _services!.CreateScope(); using var b = _services!.CreateScope();
        await Prepare(a); await Prepare(b);
        var command = Checkout("a");
        var results = await Task.WhenAll(a.ServiceProvider.GetRequiredService<IMediator>().Send(command), b.ServiceProvider.GetRequiredService<IMediator>().Send(command));
        Assert.Equal(results[0].Id, results[1].Id);
        await AssertSingleCheckout();
    }

    [PostgresFact]
    public async Task Concurrent_different_requests_cannot_buy_the_same_cart_twice()
    {
        _cartBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var a = _services!.CreateScope(); using var b = _services!.CreateScope();
        await Prepare(a); await Prepare(b);
        AssertOneConflict(await Task.WhenAll(Capture(a, Checkout("a")), Capture(b, Checkout("a"))));
        await AssertSingleCheckout();
    }

    private async Task AssertSingleCheckout()
    {
        await WithDb(async db =>
        {
            Assert.Equal(1, await db.Orders.CountAsync());
            Assert.Equal(1, await db.Payments.CountAsync());
            Assert.Equal(0, (await db.Products.FindAsync(_first))!.StockQuantity);
            Assert.Equal(1, await db.OutboxMessages.CountAsync(value => value.Type.EndsWith("OrderCreatedIntegrationEvent")));
        });
    }

    private void AddCart(string key, Guid product)
    {
        var cart = new Cart { Id = key };
        cart.AddItem(product, "Test", 1, 1);
        _carts[key] = cart;
    }

    private CreateOrderCommand Checkout(string cart, string? coupon = null)
        => new() { CheckoutRequestId = Guid.NewGuid(), UserId = _customer, CartId = cart, ShippingAddressId = _address, PaymentMethod = PaymentMethod.BankTransfer, CouponCode = coupon };

    private async Task WithDb(Func<ApplicationDbContext, Task> action)
    {
        using var scope = _services!.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    private async Task<T> Send<T>(IRequest<T> command)
    {
        using var scope = _services!.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
    }

    private async Task<Exception?> Capture<T>(IServiceScope scope, IRequest<T> command)
    {
        try { await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command); return null; }
        catch (Exception exception) { return exception; }
    }

    private async Task Prepare(IServiceScope scope, Guid? order = null)
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Products.ToListAsync();
        await db.Coupons.ToListAsync();
        if (order.HasValue)
            await db.Orders.Include(value => value.OrderItems).ThenInclude(value => value.Product).Include(value => value.Payment).SingleAsync(value => value.Id == order.Value);
    }

    private static void AssertOneConflict(Exception?[] results)
    {
        Assert.Single(results, value => value is null);
        Assert.IsType<ConflictException>(Assert.Single(results, value => value is not null));
    }

    [PostgresFact]
    public async Task Last_stock_unit_is_sold_once_and_losing_cart_is_preserved()
    {
        using var a = _services!.CreateScope(); using var b = _services!.CreateScope();
        await Prepare(a); await Prepare(b);
        AssertOneConflict(await Task.WhenAll(Capture(a, Checkout("a")), Capture(b, Checkout("b"))));
        Assert.Single(_deleted);
        await WithDb(async db =>
        {
            Assert.Equal(0, (await db.Products.FindAsync(_first))!.StockQuantity);
            Assert.Equal(1, await db.Orders.CountAsync());
            Assert.Equal(1, await db.Payments.CountAsync());
        });
    }

    [PostgresFact]
    public async Task Losing_checkout_can_retry_with_fresh_stock_when_units_remain()
    {
        await WithDb(async db => { (await db.Products.FindAsync(_first))!.StockQuantity = 2; await db.SaveChangesAsync(); });
        using var a = _services!.CreateScope(); using var b = _services!.CreateScope();
        await Prepare(a); await Prepare(b);
        AssertOneConflict(await Task.WhenAll(Capture(a, Checkout("a")), Capture(b, Checkout("b"))));
        var loser = _deleted.ContainsKey("a") ? "b" : "a";
        await Send(Checkout(loser));
        Assert.Equal(2, _deleted.Count);
        await WithDb(async db =>
        {
            Assert.Equal(0, (await db.Products.FindAsync(_first))!.StockQuantity);
            Assert.Equal(2, await db.Orders.CountAsync());
            Assert.Equal(2, await db.Payments.CountAsync());
        });
    }

    [PostgresFact]
    public async Task Last_coupon_use_is_claimed_once_and_losing_product_write_rolls_back()
    {
        AddCart("b", _second);
        await WithDb(async db =>
        {
            db.Coupons.Add(new Coupon { Code = "LAST", DiscountType = DiscountType.FixedAmount, DiscountValue = 10, MaxUsageCount = 1, ExpiryDate = DateTime.UtcNow.AddDays(1) });
            await db.SaveChangesAsync();
        });
        using var a = _services!.CreateScope(); using var b = _services!.CreateScope();
        await Prepare(a); await Prepare(b);
        AssertOneConflict(await Task.WhenAll(Capture(a, Checkout("a", "LAST")), Capture(b, Checkout("b", "LAST"))));
        await WithDb(async db =>
        {
            Assert.Equal(1, (await db.Coupons.SingleAsync()).CurrentUsageCount);
            Assert.Equal(1, await db.Orders.CountAsync());
            Assert.Equal(1, await db.Payments.CountAsync());
            Assert.Equal(1, await db.Products.SumAsync(value => value.StockQuantity));
        });
        var loser = _deleted.ContainsKey("a") ? "b" : "a";
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(Checkout(loser, "LAST")));
        Assert.Single(_deleted);
    }

    [PostgresFact]
    public async Task Concurrent_cancellations_restore_stock_only_once()
    {
        var order = await Send(Checkout("a"));
        using var a = _services!.CreateScope(); using var b = _services!.CreateScope();
        await Prepare(a, order.Id); await Prepare(b, order.Id);
        var command = new CancelOrderCommand { OrderId = order.Id, UserId = _customer };
        AssertOneConflict(await Task.WhenAll(Capture(a, command), Capture(b, command)));
        await WithDb(async db =>
        {
            Assert.Equal(1, (await db.Products.FindAsync(_first))!.StockQuantity);
            Assert.Equal(OrderStatus.Cancelled, (await db.Orders.SingleAsync()).Status);
            Assert.Equal(PaymentStatus.Failed, (await db.Payments.SingleAsync()).Status);
        });
    }

    [PostgresFact]
    public async Task Concurrent_refunds_restore_stock_only_once()
    {
        var order = await Send(Checkout("a"));
        foreach (var action in new[] { "confirm", "ship", "deliver" })
            await Send(new UpdateOrderStatusCommand { OrderId = order.Id, RequesterId = _customer, IsAdmin = true, Action = action });
        using var a = _services!.CreateScope(); using var b = _services!.CreateScope();
        await Prepare(a, order.Id); await Prepare(b, order.Id);
        var command = new RefundPaymentCommand { OrderId = order.Id };
        AssertOneConflict(await Task.WhenAll(Capture(a, command), Capture(b, command)));
        await WithDb(async db =>
        {
            Assert.Equal(1, (await db.Products.FindAsync(_first))!.StockQuantity);
            Assert.Equal(OrderStatus.Refunded, (await db.Orders.SingleAsync()).Status);
            Assert.Equal(PaymentStatus.Refunded, (await db.Payments.SingleAsync()).Status);
        });
    }

    [PostgresFact]
    public async Task Checkouts_for_different_products_both_succeed()
    {
        AddCart("b", _second);
        using var a = _services!.CreateScope(); using var b = _services!.CreateScope();
        await Prepare(a); await Prepare(b);
        Assert.All(await Task.WhenAll(Capture(a, Checkout("a")), Capture(b, Checkout("b"))), result => Assert.Null(result));
        await WithDb(async db =>
        {
            Assert.Equal(0, await db.Products.SumAsync(value => value.StockQuantity));
            Assert.Equal(2, await db.Orders.CountAsync());
            Assert.Equal(2, await db.Payments.CountAsync());
        });
    }
}