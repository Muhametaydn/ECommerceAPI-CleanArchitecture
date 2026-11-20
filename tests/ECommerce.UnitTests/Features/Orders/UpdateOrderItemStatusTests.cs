using AutoMapper;
using ECommerce.Application.Features.Orders.Commands.UpdateOrderItemStatus;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Orders;

public class UpdateOrderItemStatusTests
{
    [Fact]
    public async Task Handle_ShouldAllowSellerToPrepareOwnItem_InMixedSellerOrder()
    {
        var sellerId = Guid.NewGuid();
        var ownedItem = CreateItem(sellerId);
        var otherItem = CreateItem(Guid.NewGuid());
        var order = CreateOrder(ownedItem, otherItem);
        var handler = CreateHandler(order);

        await handler.Handle(new UpdateOrderItemStatusCommand
        {
            OrderId = order.Id,
            OrderItemId = ownedItem.Id,
            RequesterId = sellerId,
            Action = "prepare"
        }, CancellationToken.None);

        ownedItem.Status.Should().Be(OrderItemStatus.Preparing);
        otherItem.Status.Should().Be(OrderItemStatus.Pending);
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task Handle_ShouldRejectSellerForAnotherSellersItem()
    {
        var item = CreateItem(Guid.NewGuid());
        var order = CreateOrder(item);
        var handler = CreateHandler(order);

        var act = () => handler.Handle(new UpdateOrderItemStatusCommand
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            RequesterId = Guid.NewGuid(),
            Action = "prepare"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*yetkiniz yok*");
        item.Status.Should().Be(OrderItemStatus.Pending);
    }

    [Fact]
    public void SynchronizeStatusFromItems_ShouldShipOrder_WhenAllItemsAreShipped()
    {
        var first = CreateItem(Guid.NewGuid());
        var second = CreateItem(Guid.NewGuid());
        var order = CreateOrder(first, second);
        order.Confirm();
        first.StartPreparing();
        first.Ship();
        second.StartPreparing();
        second.Ship();

        order.SynchronizeStatusFromItems();

        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task Seller_cannot_start_preparing_an_order_held_by_fraud_review_in_enforce_mode()
    {
        // "prepare" siparişi onaylar ve ödemeyi tamamlar: onayla aynı fraud kontrol noktasından geçmeli.
        var sellerId = Guid.NewGuid();
        var item = CreateItem(sellerId);
        var order = CreateOrder(item);
        order.Payment = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 10 };
        order.ApplyRiskAssessment(requiresReview: true);
        var handler = CreateHandler(order, new ECommerce.Application.Common.Fraud.FraudDetectionSettings
        {
            Enabled = true, Mode = ECommerce.Application.Common.Fraud.FraudDetectionMode.Enforce
        });

        var act = () => handler.Handle(new UpdateOrderItemStatusCommand
        {
            OrderId = order.Id, OrderItemId = item.Id, RequesterId = sellerId, Action = "prepare"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ek kontrol*");
        item.Status.Should().Be(OrderItemStatus.Pending);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Payment.Status.Should().Be(PaymentStatus.Pending);
    }

    private static UpdateOrderItemStatusCommandHandler CreateHandler(Order order,
        ECommerce.Application.Common.Fraud.FraudDetectionSettings? fraud = null)
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(value => value.GetOrderWithItemsAsync(order.Id)).ReturnsAsync(order);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(value => value.Order).Returns(repository.Object);

        var mapper = new Mock<IMapper>();
        mapper.Setup(value => value.Map<OrderItemDTO>(It.IsAny<OrderItem>()))
            .Returns((OrderItem item) => new OrderItemDTO
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                Status = item.Status
            });

        return new UpdateOrderItemStatusCommandHandler(unitOfWork.Object, mapper.Object,
            new ECommerce.Application.Common.Fraud.FraudGate(fraud ?? new ECommerce.Application.Common.Fraud.FraudDetectionSettings()));
    }

    private static Order CreateOrder(params OrderItem[] items)
    {
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-ITEM-TEST" };
        foreach (var item in items)
        {
            item.OrderId = order.Id;
            item.Order = order;
            order.OrderItems.Add(item);
        }

        return order;
    }

    private static OrderItem CreateItem(Guid sellerId)
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Test ürünü" };
        product.AssignSeller(sellerId, autoApprove: true);
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Quantity = 1,
            UnitPrice = 100
        };
    }
}
