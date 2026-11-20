using ECommerce.Infrastructure.Events;
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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ECommerce.IntegrationTests;

// Real handlers, validators, mappings, repositories and relational transactions.
// Only the Redis cart and response cache are replaced; no live data is used.
public sealed class OrderPaymentPersistenceTests : IAsyncLifetime
{
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<IUserLookupService> _users = new();
    // Varsayılan kapalı; fraud testleri kendi modunu açar.
    private readonly ECommerce.Application.Common.Fraud.FraudDetectionSettings _fraud = new();
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ServiceProvider _services = null!;
    private Cart? _cart;
    private bool _cartDeleted;
    private bool _failCleanup;
    private Guid _customer, _otherCustomer, _address, _otherAddress;
    private readonly Guid _product = Guid.NewGuid();
    private readonly Guid _secondProduct = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddApplicationServices();
        services.AddPersistenceServices(new ConfigurationBuilder().Build());
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        services.AddSingleton(_cache.Object);
        services.AddSingleton(_users.Object);
        services.AddSingleton(_fraud);
        services.AddSingleton(Mock.Of<ISearchService>());
        services.AddSingleton(Mock.Of<IProductImageStorage>());
        var cartService = new Mock<ICartService>();
        cartService.Setup(value => value.GetCartAsync(It.IsAny<string>())).Returns(() => Task.FromResult(_cart));
        cartService.Setup(value => value.DeleteCartIfUnchangedAsync(It.IsAny<string>(), It.IsAny<Cart>())).Callback(() => { if (_failCleanup) throw new InvalidOperationException("Simulated Redis cleanup failure"); _cart = null; _cartDeleted = true; }).ReturnsAsync(true);
        services.AddSingleton(cartService.Object);
        _services = services.BuildServiceProvider();
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        var customer = ApplicationUser.Create("Test", "Customer", "customer@example.invalid", "customer");
        var other = ApplicationUser.Create("Other", "Customer", "other@example.invalid", "other");
        _customer = customer.Id;
        _otherCustomer = other.Id;
        db.Set<ApplicationUser>().AddRange(customer, other);
        var address = new Address { Id = Guid.NewGuid(), UserId = _customer, AddressLine = "Own address", City = "Istanbul", District = "Test" };
        var otherAddress = new Address { Id = Guid.NewGuid(), UserId = _otherCustomer, AddressLine = "Other address", City = "Istanbul", District = "Test" };
        _address = address.Id;
        _otherAddress = otherAddress.Id;
        db.Address.AddRange(address, otherAddress);
        var category = Guid.Parse("a1111111-1111-1111-1111-111111111111");
        db.Products.AddRange(
            new Product { Id = _product, Name = "First", SKU = "FIRST", CategoryId = category, Price = 100, StockQuantity = 10 },
            new Product { Id = _secondProduct, Name = "Second", SKU = "SECOND", CategoryId = category, Price = 200, StockQuantity = 5 });
        await db.SaveChangesAsync();
        _cart = new Cart { Id = "test-cart" };
        _cart.AddItem(_product, "Stale name", 1, 2);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private Task<OrderDetailDTO> Create(Guid? address = null, string? coupon = null)
        => Send(new CreateOrderCommand { CheckoutRequestId = Guid.NewGuid(), UserId = _customer, CartId = "test-cart", ShippingAddressId = address ?? _address, PaymentMethod = PaymentMethod.BankTransfer, CouponCode = coupon });

    private async Task<T> Send<T>(IRequest<T> command)
    {
        using var scope = _services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
    }

    private async Task WithDb(Func<ApplicationDbContext, Task> action)
    {
        using var scope = _services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    private void UseProductCache()
    {
        ECommerce.Application.Features.Products.DTOs.ProductDTO? cached = null;
        var key = $"products:single:{_product}";
        _cache.Setup(cache => cache.GetAsync<ECommerce.Application.Features.Products.DTOs.ProductDTO>(key, It.IsAny<CancellationToken>())).Returns(() => Task.FromResult(cached));
        _cache.Setup(cache => cache.SetAsync(key, It.IsAny<ECommerce.Application.Features.Products.DTOs.ProductDTO>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, ECommerce.Application.Features.Products.DTOs.ProductDTO, TimeSpan?, CancellationToken>((_, value, _, _) => cached = value).Returns(Task.CompletedTask);
        _cache.Setup(cache => cache.RemoveAsync(key, It.IsAny<CancellationToken>())).Callback(() => cached = null).Returns(Task.CompletedTask);
    }

    private Task<ECommerce.Application.Features.Products.DTOs.ProductDTO> ReadProduct()
        => Send(new ECommerce.Application.Features.Products.Queries.GetProductById.GetProductByIdQuery(_product));

    [Fact]
    public async Task Warm_product_cache_reflects_checkout_and_cancellation_stock()
    {
        UseProductCache();
        Assert.Equal(10, (await ReadProduct()).StockQuantity);
        var order = await Create();
        Assert.Equal(8, (await ReadProduct()).StockQuantity);
        await Send(new CancelOrderCommand { OrderId = order.Id, UserId = _customer });
        Assert.Equal(10, (await ReadProduct()).StockQuantity);
        _cache.Verify(cache => cache.RemoveByPrefixAsync("products:list:", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Cache_failure_does_not_turn_committed_checkout_into_an_error()
    {
        _cache.Setup(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Simulated cache outage"));
        var order = await Create();
        Assert.True(_cartDeleted);
        await WithDb(async db => { Assert.Equal(order.Id, (await db.Orders.SingleAsync()).Id); Assert.Equal(8, (await db.Products.FindAsync(_product))!.StockQuantity); });
    }
    [Fact]
    public async Task Checkout_saves_payment_current_price_and_stock_before_clearing_cart()
    {
        var created = await Create();
        Assert.True(_cartDeleted);
        Assert.Equal(200, created.TotalAmount);
        Assert.Equal(PaymentStatus.Pending, created.PaymentStatus);
        Assert.Equal(PaymentMethod.BankTransfer, created.PaymentMethod);
        await WithDb(async db =>
        {
            var payment = await db.Payments.SingleAsync();
            Assert.Equal(created.Id, payment.OrderId);
            Assert.Equal(200, payment.Amount);
            Assert.Equal(8, (await db.Products.FindAsync(_product))!.StockQuantity);
            Assert.Equal(100, (await db.OrderItems.SingleAsync()).UnitPrice);
        });
    }

    [Fact]
    public async Task Checkout_rejects_another_customers_address_without_writes()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => Create(_otherAddress));
        await AssertCheckoutUnchanged();
    }

    [Fact]
    public async Task Checkout_rejects_missing_address_without_writes()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Create(Guid.NewGuid()));
        await AssertCheckoutUnchanged();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Checkout_rejects_missing_or_empty_cart(bool empty)
    {
        _cart = empty ? new Cart() : null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create());
        await AssertCheckoutUnchanged();
    }

    [Fact]
    public async Task Later_item_with_insufficient_stock_leaves_every_product_unchanged()
    {
        _cart!.AddItem(_secondProduct, "Second", 200, 6);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create());
        await AssertCheckoutUnchanged();
    }

    [Fact]
    public async Task Inactive_product_cannot_be_ordered()
    {
        await WithDb(async db => { (await db.Products.FindAsync(_product))!.IsActive = false; await db.SaveChangesAsync(); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create());
        await AssertCheckoutUnchanged();
    }

    [Fact]
    public async Task Missing_coupon_leaves_stock_and_cart_unchanged()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(coupon: "MISSING"));
        await AssertCheckoutUnchanged();
    }

    [Fact]
    public async Task Coupon_discount_is_saved_in_order_payment_and_usage_count()
    {
        await WithDb(async db =>
        {
            db.Coupons.Add(new Coupon { Code = "SAVE", DiscountType = DiscountType.Percentage, DiscountValue = 10, MaxUsageCount = 5, ExpiryDate = DateTime.UtcNow.AddDays(1) });
            await db.SaveChangesAsync();
        });
        var created = await Create(coupon: "save");
        Assert.Equal(180, created.TotalAmount);
        await WithDb(async db =>
        {
            Assert.Equal(180, (await db.Payments.SingleAsync()).Amount);
            Assert.Equal(1, (await db.Coupons.SingleAsync()).CurrentUsageCount);
        });
    }

    [Fact]
    public async Task Failed_database_save_does_not_clear_cart_or_leave_partial_stock()
    {
        await WithDb(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_payment BEFORE INSERT ON Payments BEGIN SELECT RAISE(ABORT, 'test payment failure'); END;");
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => Create());
        await AssertCheckoutUnchanged();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_restores_stock_once_and_updates_payment(bool paid)
    {
        var order = await Create();
        if (paid) await Progress(order.Id, "confirm");
        await Send(new CancelOrderCommand { OrderId = order.Id, UserId = _customer });
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(new CancelOrderCommand { OrderId = order.Id, UserId = _customer }));
        await WithDb(async db =>
        {
            Assert.Equal(10, (await db.Products.FindAsync(_product))!.StockQuantity);
            Assert.Equal(OrderStatus.Cancelled, (await db.Orders.SingleAsync()).Status);
            Assert.Equal(OrderItemStatus.Cancelled, (await db.OrderItems.SingleAsync()).Status);
            Assert.Equal(paid ? PaymentStatus.Refunded : PaymentStatus.Failed, (await db.Payments.SingleAsync()).Status);
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(new RefundPaymentCommand { OrderId = order.Id }));
        await WithDb(async db => Assert.Equal(10, (await db.Products.FindAsync(_product))!.StockQuantity));
    }

    [Fact]
    public async Task Shipped_order_cannot_be_cancelled_or_refunded()
    {
        var order = await Create();
        await Progress(order.Id, "confirm");
        await Progress(order.Id, "ship");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(new CancelOrderCommand { OrderId = order.Id, UserId = _customer }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(new RefundPaymentCommand { OrderId = order.Id }));
        await WithDb(async db =>
        {
            Assert.Equal(8, (await db.Products.FindAsync(_product))!.StockQuantity);
            Assert.Equal(OrderStatus.Shipped, (await db.Orders.SingleAsync()).Status);
            Assert.Equal(PaymentStatus.Completed, (await db.Payments.SingleAsync()).Status);
        });
    }

    [Fact]
    public async Task Delivered_paid_order_refunds_and_restores_stock_exactly_once()
    {
        var order = await Create();
        foreach (var action in new[] { "confirm", "ship", "deliver" }) await Progress(order.Id, action);
        var refund = await Send(new RefundPaymentCommand { OrderId = order.Id });
        Assert.Equal(PaymentStatus.Refunded, refund.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(new RefundPaymentCommand { OrderId = order.Id }));
        await WithDb(async db =>
        {
            Assert.Equal(10, (await db.Products.FindAsync(_product))!.StockQuantity);
            Assert.Equal(OrderStatus.Refunded, (await db.Orders.SingleAsync()).Status);
            Assert.Equal(PaymentStatus.Refunded, (await db.Payments.SingleAsync()).Status);
        });
    }

    private CreateOrderCommand Attempt(Guid? requestId = null)
        => new() { CheckoutRequestId = requestId ?? Guid.NewGuid(), UserId = _customer, CartId = "test-cart", ShippingAddressId = _address, PaymentMethod = PaymentMethod.BankTransfer };

    [Fact]
    public async Task Repeated_request_after_cart_deletion_returns_the_same_order()
    {
        var command = Attempt();
        var first = await Send(command);
        var second = await Send(command);
        Assert.Equal(first.Id, second.Id);
        await WithDb(async db =>
        {
            Assert.Equal(1, await db.Orders.CountAsync());
            Assert.Equal(1, await db.Payments.CountAsync());
            Assert.Equal(8, (await db.Products.FindAsync(_product))!.StockQuantity);
            Assert.Equal(1, await db.OutboxMessages.CountAsync(value => value.Type.EndsWith("OrderCreatedIntegrationEvent")));
        });
    }

    [Fact]
    public async Task Changing_payment_on_the_same_request_is_rejected()
    {
        var command = Attempt();
        await Send(command);
        command.PaymentMethod = PaymentMethod.CreditCard;
        await Assert.ThrowsAsync<ConflictException>(() => Send(command));
    }

    [Fact]
    public async Task Committed_order_survives_cleanup_failure_and_retry_cleans_cart()
    {
        _failCleanup = true;
        var command = Attempt();
        var first = await Send(command);
        Assert.False(_cartDeleted);
        _failCleanup = false;
        var second = await Send(command);
        Assert.Equal(first.Id, second.Id);
        Assert.True(_cartDeleted);
        await WithDb(async db => Assert.Equal(1, await db.Orders.CountAsync()));
    }

    [Fact]
    public async Task Another_request_cannot_purchase_a_retained_cart_again()
    {
        _failCleanup = true;
        await Send(Attempt());
        _failCleanup = false;
        await Assert.ThrowsAsync<ConflictException>(() => Send(Attempt()));
        Assert.True(_cartDeleted);
        await WithDb(async db =>
        {
            Assert.Equal(1, await db.Orders.CountAsync());
            Assert.Equal(8, (await db.Products.FindAsync(_product))!.StockQuantity);
        });
    }

    [Fact]
    public async Task Replay_does_not_delete_a_changed_cart()
    {
        _failCleanup = true;
        var command = Attempt();
        var first = await Send(command);
        _cart!.AddItem(_secondProduct, "Second", 200, 1);
        _failCleanup = false;
        Assert.Equal(first.Id, (await Send(command)).Id);
        Assert.False(_cartDeleted);
        Assert.Equal(2, _cart!.Items.Count);
    }

    [Fact]
    public async Task Outbox_insert_failure_rolls_back_order_payment_stock_and_keeps_cart()
    {
        await WithDb(async db => await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_outbox BEFORE INSERT ON OutboxMessages BEGIN SELECT RAISE(ABORT, 'test outbox failure'); END;"));
        await Assert.ThrowsAsync<DbUpdateException>(() => Send(Attempt()));
        await AssertCheckoutUnchanged();
        await WithDb(async db => Assert.Empty(await db.OutboxMessages.ToListAsync()));
    }

    [Fact]
    public async Task Missing_request_id_is_rejected_before_checkout()
    {
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => Send(Attempt(Guid.Empty)));
        await AssertCheckoutUnchanged();
    }

    private Task<OrderDetailDTO> Progress(Guid order, string action)
        => Send(new UpdateOrderStatusCommand { OrderId = order, RequesterId = Guid.NewGuid(), IsAdmin = true, Action = action });

    private async Task AssertCheckoutUnchanged()
    {
        Assert.False(_cartDeleted);
        await WithDb(async db =>
        {
            Assert.Empty(await db.Orders.ToListAsync());
            Assert.Empty(await db.Payments.ToListAsync());
            Assert.Equal(10, (await db.Products.FindAsync(_product))!.StockQuantity);
            Assert.Equal(5, (await db.Products.FindAsync(_secondProduct))!.StockQuantity);
        });
    }

    [Fact]
    public async Task Deleting_an_ordered_product_keeps_order_history_and_deactivates_it()
    {
        // Regression: OrderItem→Product FK Cascade idi; ürün silmek sipariş kalemlerini siliyordu.
        var order = await Create();

        await Send(new ECommerce.Application.Features.Products.Commands.DeleteProduct.DeleteProductCommand(_product));

        await WithDb(async db =>
        {
            var product = await db.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == _product);
            Assert.False(product.IsActive);
            var items = await db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
            Assert.Single(items);
            Assert.Equal(_product, items[0].ProductId);
        });
    }

    [Fact]
    public async Task Deleting_a_never_ordered_product_removes_it()
    {
        await Send(new ECommerce.Application.Features.Products.Commands.DeleteProduct.DeleteProductCommand(_secondProduct));

        await WithDb(async db => Assert.False(await db.Products.IgnoreQueryFilters().AnyAsync(p => p.Id == _secondProduct)));
    }

    [Fact]
    public async Task Cancelling_an_order_returns_its_coupon_use()
    {
        await WithDb(async db =>
        {
            db.Coupons.Add(new Coupon { Id = Guid.NewGuid(), Code = "TEKSEFER", DiscountType = DiscountType.FixedAmount, DiscountValue = 10, MaxUsageCount = 1, ExpiryDate = DateTime.UtcNow.AddDays(1) });
            await db.SaveChangesAsync();
        });
        var order = await Create(coupon: "TEKSEFER");
        await WithDb(async db => Assert.Equal(1, (await db.Coupons.SingleAsync()).CurrentUsageCount));

        await Send(new CancelOrderCommand { OrderId = order.Id, UserId = _customer });

        await WithDb(async db => Assert.Equal(0, (await db.Coupons.SingleAsync()).CurrentUsageCount));
    }


    // ── Fraud entegrasyonu (Faz 1) ───────────────────────────────────────────

    private const string RiskCheckType = "ECommerce.Contracts.Fraud.OrderRiskCheckRequested";

    private void UseFraud(ECommerce.Application.Common.Fraud.FraudDetectionMode mode)
    {
        _fraud.Enabled = true;
        _fraud.Mode = mode;
        _users.Setup(users => users.GetRiskProfileAsync(_customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRiskProfile(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), "test@example.invalid", true));
    }

    private Task<ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision.RiskDecisionResult> Assess(Guid order, string decision)
        => Send(new ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision.ApplyRiskAssessmentCommand(order, Guid.NewGuid(), decision, 75));

    private Task<ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision.RiskDecisionResult> Review(Guid order, string outcome)
        => Send(new ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision.ApplyRiskReviewCommand(order, Guid.NewGuid(), outcome, Guid.NewGuid()));

    private async Task<RiskStatus> RiskOf(Guid order)
    {
        var status = RiskStatus.NotChecked;
        await WithDb(async db => status = (await db.Orders.AsNoTracking().SingleAsync(o => o.Id == order)).RiskStatus);
        return status;
    }

    [Fact]
    public async Task Fraud_off_writes_no_risk_event_and_never_holds_orders()
    {
        var order = await Create();

        await WithDb(async db => Assert.False(await db.OutboxMessages.AnyAsync(m => m.Type == RiskCheckType)));
        Assert.Equal(RiskStatus.NotChecked, await RiskOf(order.Id));
        _users.Verify(users => users.GetRiskProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(OrderStatus.Confirmed, (await Progress(order.Id, "confirm")).Status);
    }

    [Fact]
    public async Task Fraud_on_writes_the_risk_event_in_the_order_transaction()
    {
        UseFraud(ECommerce.Application.Common.Fraud.FraudDetectionMode.Monitor);

        var order = await Send(new CreateOrderCommand
        {
            CheckoutRequestId = Guid.NewGuid(), UserId = _customer, CartId = "test-cart", ShippingAddressId = _address,
            PaymentMethod = PaymentMethod.CreditCard, ClientIpAddress = "203.0.113.5", UserAgent = "IntegrationTest/1.0"
        });

        await WithDb(async db =>
        {
            var outbox = await db.OutboxMessages.SingleAsync(m => m.Type == RiskCheckType);
            var message = System.Text.Json.JsonSerializer.Deserialize<ECommerce.Contracts.Fraud.OrderRiskCheckRequested>(outbox.Payload)!;
            Assert.Equal(order.Id, message.OrderId);
            Assert.Equal(_customer, message.Customer.UserId);
            Assert.Equal("example.invalid", message.Customer.EmailDomain);
            Assert.Equal("203.0.113.5", message.Channel.IpAddress);
            Assert.Equal("IntegrationTest/1.0", message.Channel.UserAgent);
            Assert.Equal(_address, message.ShippingAddress.AddressId);
            var item = Assert.Single(message.Items);
            Assert.Equal(_product, item.ProductId);
            Assert.Equal(Guid.Parse("a1111111-1111-1111-1111-111111111111"), item.CategoryId);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(100m, item.UnitPrice);
            Assert.Equal(order.TotalAmount, message.TotalAmount);
            Assert.Equal(ECommerce.Contracts.Fraud.PaymentMethods.CreditCard, message.PaymentMethod);
        });
    }

    [Fact]
    public async Task Failed_checkout_writes_no_risk_event()
    {
        UseFraud(ECommerce.Application.Common.Fraud.FraudDetectionMode.Enforce);

        await Assert.ThrowsAsync<ForbiddenException>(() => Create(_otherAddress));

        await WithDb(async db => Assert.False(await db.OutboxMessages.AnyAsync(m => m.Type == RiskCheckType)));
    }

    [Fact]
    public async Task Enforce_holds_a_reviewed_order_until_the_analyst_approves()
    {
        UseFraud(ECommerce.Application.Common.Fraud.FraudDetectionMode.Enforce);
        var order = await Create();
        Assert.Equal(ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision.RiskDecisionResult.Applied,
            await Assess(order.Id, ECommerce.Contracts.Fraud.RiskDecisions.Review));

        var held = await Assert.ThrowsAsync<InvalidOperationException>(() => Progress(order.Id, "confirm"));
        Assert.Contains("ek kontrol", held.Message);
        await WithDb(async db =>
        {
            Assert.Equal(OrderStatus.Pending, (await db.Orders.SingleAsync()).Status);
            Assert.Equal(PaymentStatus.Pending, (await db.Payments.SingleAsync()).Status);
        });

        await Review(order.Id, ECommerce.Contracts.Fraud.ReviewOutcomes.Approved);

        Assert.Equal(OrderStatus.Confirmed, (await Progress(order.Id, "confirm")).Status);
        Assert.Equal(RiskStatus.Clear, await RiskOf(order.Id));
    }

    [Fact]
    public async Task Enforce_rejection_cancels_the_order_and_restores_stock()
    {
        UseFraud(ECommerce.Application.Common.Fraud.FraudDetectionMode.Enforce);
        var order = await Create();
        await Assess(order.Id, ECommerce.Contracts.Fraud.RiskDecisions.Review);

        await Review(order.Id, ECommerce.Contracts.Fraud.ReviewOutcomes.Rejected);

        await WithDb(async db =>
        {
            var saved = await db.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Cancelled, saved.Status);
            Assert.Equal(RiskStatus.Rejected, saved.RiskStatus);
            Assert.Equal(PaymentStatus.Failed, (await db.Payments.SingleAsync()).Status);
            Assert.Equal(10, (await db.Products.FindAsync(_product))!.StockQuantity);
        });
    }

    [Fact]
    public async Task Monitor_records_decisions_but_never_blocks_or_cancels()
    {
        UseFraud(ECommerce.Application.Common.Fraud.FraudDetectionMode.Monitor);
        var order = await Create();
        await Assess(order.Id, ECommerce.Contracts.Fraud.RiskDecisions.Review);

        Assert.Equal(OrderStatus.Confirmed, (await Progress(order.Id, "confirm")).Status);
        await Review(order.Id, ECommerce.Contracts.Fraud.ReviewOutcomes.Rejected);

        await WithDb(async db =>
        {
            var saved = await db.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Confirmed, saved.Status);
            Assert.Equal(RiskStatus.Rejected, saved.RiskStatus);
            Assert.Equal(8, (await db.Products.FindAsync(_product))!.StockQuantity);
        });
    }

    [Fact]
    public async Task Enforce_fails_open_while_no_decision_has_arrived()
    {
        UseFraud(ECommerce.Application.Common.Fraud.FraudDetectionMode.Enforce);
        var order = await Create();

        Assert.Equal(OrderStatus.Confirmed, (await Progress(order.Id, "confirm")).Status);
    }

    [Fact]
    public async Task Late_assessment_does_not_override_the_analyst_or_unknown_orders()
    {
        UseFraud(ECommerce.Application.Common.Fraud.FraudDetectionMode.Enforce);
        var order = await Create();
        await Review(order.Id, ECommerce.Contracts.Fraud.ReviewOutcomes.Approved);

        Assert.Equal(ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision.RiskDecisionResult.IgnoredStale,
            await Assess(order.Id, ECommerce.Contracts.Fraud.RiskDecisions.Review));
        Assert.Equal(RiskStatus.Clear, await RiskOf(order.Id));
        Assert.Equal(ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision.RiskDecisionResult.IgnoredUnknownOrder,
            await Assess(Guid.NewGuid(), ECommerce.Contracts.Fraud.RiskDecisions.Review));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Assess(order.Id, "Maybe"));
    }

}