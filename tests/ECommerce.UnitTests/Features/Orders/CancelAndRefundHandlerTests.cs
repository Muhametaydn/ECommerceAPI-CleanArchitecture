using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Orders.Commands.CancelOrder;
using ECommerce.Application.Features.Payments.Commands.RefundPayment;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Events.Orders;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Orders;

public class CancelAndRefundHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICouponRepository> _coupons = new();
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Product _product = new() { Id = Guid.NewGuid(), Name = "Kulaklık", StockQuantity = 5 };
    private readonly Guid _customer = Guid.NewGuid();

    public CancelAndRefundHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Order).Returns(_orders.Object);
        _unitOfWork.SetupGet(u => u.Product).Returns(_products.Object);
        _unitOfWork.SetupGet(u => u.Coupon).Returns(_coupons.Object);
        _unitOfWork.SetupGet(u => u.Payment).Returns(_payments.Object);
        _products.Setup(p => p.GetByIdAsync(_product.Id)).ReturnsAsync(_product);
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_restocks_items_fails_pending_payment_and_releases_coupon()
    {
        var coupon = new Coupon { Code = "YAZ10", MaxUsageCount = 1, CurrentUsageCount = 1, ExpiryDate = DateTime.UtcNow.AddDays(1) };
        _coupons.Setup(c => c.GetByCodeAsync("YAZ10")).ReturnsAsync(coupon);
        var order = NewOrder(quantity: 2, couponCode: "YAZ10");

        await new CancelOrderCommandHandler(_unitOfWork.Object)
            .Handle(new CancelOrderCommand { OrderId = order.Id, UserId = _customer }, CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.OrderItems.Should().OnlyContain(i => i.Status == OrderItemStatus.Cancelled);
        order.Payment!.Status.Should().Be(PaymentStatus.Failed);
        _product.StockQuantity.Should().Be(7);
        // Regression: iptal edilen siparişin kupon hakkı eskiden geri verilmiyordu.
        coupon.CurrentUsageCount.Should().Be(0);
        _coupons.Verify(c => c.Update(coupon), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Cancel_refunds_completed_payment()
    {
        var order = NewOrder();
        order.Payment!.MarkAsCompleted("TXN-1");

        await new CancelOrderCommandHandler(_unitOfWork.Object)
            .Handle(new CancelOrderCommand { OrderId = order.Id, UserId = _customer }, CancellationToken.None);

        order.Payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task Cancel_of_another_users_order_is_forbidden_and_changes_nothing()
    {
        var order = NewOrder();

        var act = () => new CancelOrderCommandHandler(_unitOfWork.Object)
            .Handle(new CancelOrderCommand { OrderId = order.Id, UserId = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        order.Status.Should().Be(OrderStatus.Pending);
        _product.StockQuantity.Should().Be(5);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Cancel_with_a_shipped_item_is_rejected()
    {
        var order = NewOrder();
        var item = order.OrderItems.Single();
        item.StartPreparing();
        item.Ship();

        var act = () => new CancelOrderCommandHandler(_unitOfWork.Object)
            .Handle(new CancelOrderCommand { OrderId = order.Id, UserId = _customer }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _product.StockQuantity.Should().Be(5);
    }

    // ── Refund ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refund_marks_order_refunded_through_domain_and_restocks()
    {
        var order = NewOrder(quantity: 3);
        order.Payment!.MarkAsCompleted("TXN-1");
        order.Confirm();
        order.Ship();
        order.Deliver();
        order.ClearDomainEvents();
        _payments.Setup(p => p.GetByOrderIdAsync(order.Id)).ReturnsAsync(order.Payment);

        await new RefundPaymentCommandHandler(_unitOfWork.Object, Mock.Of<IMapper>())
            .Handle(new RefundPaymentCommand { OrderId = order.Id }, CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Refunded);
        order.Payment.Status.Should().Be(PaymentStatus.Refunded);
        _product.StockQuantity.Should().Be(8);
        // Regression: durum doğrudan atanıyordu; müşteriye iade bildirimi hiç gitmiyordu.
        order.DomainEvents.Should().ContainSingle(e => e is OrderRefundedDomainEvent);
    }

    [Fact]
    public async Task Refund_of_undelivered_order_is_rejected()
    {
        var order = NewOrder();
        order.Payment!.MarkAsCompleted("TXN-1");
        order.Confirm();
        _payments.Setup(p => p.GetByOrderIdAsync(order.Id)).ReturnsAsync(order.Payment);

        var act = () => new RefundPaymentCommandHandler(_unitOfWork.Object, Mock.Of<IMapper>())
            .Handle(new RefundPaymentCommand { OrderId = order.Id }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        order.Payment.Status.Should().Be(PaymentStatus.Completed);
        _product.StockQuantity.Should().Be(5);
    }

    [Fact]
    public async Task Refund_twice_is_rejected()
    {
        var order = NewOrder();
        order.Payment!.MarkAsCompleted("TXN-1");
        order.Payment.Refund();
        _payments.Setup(p => p.GetByOrderIdAsync(order.Id)).ReturnsAsync(order.Payment);

        var act = () => new RefundPaymentCommandHandler(_unitOfWork.Object, Mock.Of<IMapper>())
            .Handle(new RefundPaymentCommand { OrderId = order.Id }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*zaten iade*");
    }

    private Order NewOrder(int quantity = 1, string? couponCode = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = Order.GenerateOrderNumber(),
            UserId = _customer,
            CouponCode = couponCode
        };
        order.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = _product.Id,
            Product = _product,
            Quantity = quantity,
            UnitPrice = 100m
        });
        order.Payment = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 100m * quantity };
        _orders.Setup(o => o.GetOrderWithItemsAsync(order.Id)).ReturnsAsync(order);
        return order;
    }
}
