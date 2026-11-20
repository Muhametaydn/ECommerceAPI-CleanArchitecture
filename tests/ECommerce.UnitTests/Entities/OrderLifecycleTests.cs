using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Events.Orders;
using ECommerce.Domain.Events.Payments;
using FluentAssertions;

namespace ECommerce.UnitTests.Entities;

public class OrderLifecycleTests
{
    // ── Order state machine ──────────────────────────────────────────────────

    [Fact]
    public void Happy_path_moves_through_every_state_and_raises_an_event_for_each()
    {
        var order = NewOrder();

        order.Confirm();
        order.Ship();
        order.Deliver();
        order.MarkAsRefunded();

        order.Status.Should().Be(OrderStatus.Refunded);
        order.DomainEvents.Select(e => e.GetType()).Should().Equal(
            typeof(OrderConfirmedDomainEvent),
            typeof(OrderShippedDomainEvent),
            typeof(OrderDeliveredDomainEvent),
            typeof(OrderRefundedDomainEvent));
    }

    [Fact]
    public void Pending_order_cannot_skip_to_shipped_or_delivered()
    {
        var order = NewOrder();

        order.Invoking(o => o.Ship()).Should().Throw<InvalidOperationException>();
        order.Invoking(o => o.Deliver()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Pending_and_confirmed_orders_can_be_cancelled()
    {
        var pending = NewOrder();
        var confirmed = NewOrder();
        confirmed.Confirm();

        pending.Cancel();
        confirmed.Cancel();

        pending.Status.Should().Be(OrderStatus.Cancelled);
        confirmed.Status.Should().Be(OrderStatus.Cancelled);
        confirmed.DomainEvents.Should().ContainSingle(e => e is OrderCancelledDomainEvent);
    }

    [Fact]
    public void Shipped_order_cannot_be_cancelled()
    {
        // Regression: domain eskiden kargodaki siparişin iptaline izin veriyordu.
        var order = NewOrder();
        order.Confirm();
        order.Ship();

        order.Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public void Only_delivered_orders_can_be_refunded(OrderStatus status)
    {
        var order = NewOrder();
        order.Status = status;

        order.Invoking(o => o.MarkAsRefunded()).Should().Throw<InvalidOperationException>();
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CalculateTotal_subtracts_discount_and_never_goes_negative()
    {
        var order = NewOrder(unitPrice: 40m, quantity: 2);
        order.DiscountAmount = 30m;
        order.CalculateTotal();
        order.TotalAmount.Should().Be(50m);

        order.DiscountAmount = 500m;
        order.CalculateTotal();
        order.TotalAmount.Should().Be(0m);
    }

    // ── Coupon ───────────────────────────────────────────────────────────────

    [Fact]
    public void Percentage_discount_is_rounded_to_cents()
    {
        // Regression: %33 indirim 33,3333... üretiyordu; ödeme tutarında kuruş kayması olur.
        var coupon = NewCoupon(DiscountType.Percentage, 33m);

        coupon.CalculateDiscount(100.01m).Should().Be(33.00m);
        coupon.CalculateDiscount(10m).Should().Be(3.30m);
        coupon.CalculateDiscount(1.5m).Should().Be(0.50m); // 0.495 → 0.50 (AwayFromZero)
    }

    [Fact]
    public void Fixed_discount_cannot_exceed_order_total()
    {
        NewCoupon(DiscountType.FixedAmount, 150m).CalculateDiscount(100m).Should().Be(100m);
    }

    [Fact]
    public void Coupon_below_minimum_order_amount_is_rejected()
    {
        var coupon = NewCoupon(DiscountType.FixedAmount, 10m);
        coupon.MinimumOrderAmount = 200m;

        coupon.Invoking(c => c.CalculateDiscount(199.99m)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Expired_inactive_or_exhausted_coupons_are_invalid()
    {
        var expired = NewCoupon(DiscountType.FixedAmount, 10m);
        expired.ExpiryDate = DateTime.UtcNow.AddMinutes(-1);
        var inactive = NewCoupon(DiscountType.FixedAmount, 10m);
        inactive.Deactivate();
        var exhausted = NewCoupon(DiscountType.FixedAmount, 10m);
        exhausted.CurrentUsageCount = exhausted.MaxUsageCount;

        expired.IsValid().Should().BeFalse();
        inactive.IsValid().Should().BeFalse();
        exhausted.IsValid().Should().BeFalse();
        exhausted.Invoking(c => c.Use()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Release_gives_back_one_use_and_never_goes_below_zero()
    {
        var coupon = NewCoupon(DiscountType.FixedAmount, 10m);
        coupon.MaxUsageCount = 1;
        coupon.Use();
        coupon.IsValid().Should().BeFalse();

        coupon.Release();
        coupon.Release();

        coupon.CurrentUsageCount.Should().Be(0);
        coupon.IsValid().Should().BeTrue();
    }

    // ── Payment ──────────────────────────────────────────────────────────────

    [Fact]
    public void Payment_complete_then_refund_raises_completion_event()
    {
        var payment = new Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 10m };

        payment.MarkAsCompleted("TXN-1");
        payment.Refund();

        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.DomainEvents.Should().ContainSingle(e => e is PaymentCompletedDomainEvent);
        payment.Invoking(p => p.Refund()).Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Failed)]
    public void Only_pending_payment_can_be_marked_failed(PaymentStatus status)
    {
        // Regression: tamamlanmış ödeme "başarısız" yapılabiliyordu.
        var payment = new Payment { Status = status };

        payment.Invoking(p => p.MarkAsFailed()).Should().Throw<InvalidOperationException>();
        payment.Status.Should().Be(status);
    }

    // ── Product stock ────────────────────────────────────────────────────────

    [Fact]
    public void Stock_cannot_go_negative_and_quantities_must_be_positive()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Kalem", StockQuantity = 3 };

        product.Invoking(p => p.DecreaseStock(4)).Should().Throw<ArgumentException>();
        product.Invoking(p => p.DecreaseStock(0)).Should().Throw<ArgumentException>();
        product.Invoking(p => p.IncreaseStock(-1)).Should().Throw<ArgumentException>();

        product.DecreaseStock(3);
        product.StockQuantity.Should().Be(0);
        product.IncreaseStock(2);
        product.StockQuantity.Should().Be(2);
    }

    [Fact]
    public void Price_cannot_be_negative()
    {
        new Product().Invoking(p => p.UpdatePrice(-0.01m)).Should().Throw<ArgumentException>();
    }

    private static Order NewOrder(decimal unitPrice = 10m, int quantity = 1)
    {
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = Order.GenerateOrderNumber(), UserId = Guid.NewGuid() };
        order.OrderItems.Add(new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = Guid.NewGuid(), Quantity = quantity, UnitPrice = unitPrice });
        return order;
    }

    private static Coupon NewCoupon(DiscountType type, decimal value) => new()
    {
        Id = Guid.NewGuid(),
        Code = "TEST",
        DiscountType = type,
        DiscountValue = value,
        MaxUsageCount = 10,
        ExpiryDate = DateTime.UtcNow.AddDays(1)
    };
}
