using AutoMapper;
using ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Orders;

public class UpdateOrderStatusSellerAuthorizationTests
{
    [Fact]
    public async Task Handle_ShouldRejectSeller_WhenOrderHasNoOwnedItems()
    {
        var sellerId = Guid.NewGuid();
        var order = CreateOrderWithSeller(Guid.NewGuid());
        var handler = CreateHandler(order);
        var command = new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            RequesterId = sellerId,
            IsAdmin = false,
            Action = "confirm"
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*yetkiniz yok*");
    }

    [Fact]
    public async Task Handle_ShouldRejectSeller_WhenOrderContainsAnotherSellersItems()
    {
        var sellerId = Guid.NewGuid();
        var order = CreateOrderWithSeller(sellerId);
        var otherProduct = new Product { Id = Guid.NewGuid(), Name = "Diğer ürün" };
        otherProduct.AssignSeller(Guid.NewGuid(), autoApprove: true);
        order.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = otherProduct.Id,
            Product = otherProduct,
            Quantity = 1,
            UnitPrice = 100
        });
        var handler = CreateHandler(order);
        var command = new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            RequesterId = sellerId,
            IsAdmin = false,
            Action = "confirm"
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*birden fazla satıcının*");
    }

    private static UpdateOrderStatusCommandHandler CreateHandler(Order order)
    {
        var orderRepository = new Mock<IOrderRepository>();
        orderRepository
            .Setup(repository => repository.GetOrderWithItemsAsync(order.Id))
            .ReturnsAsync(order);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(value => value.Order).Returns(orderRepository.Object);
        return new UpdateOrderStatusCommandHandler(unitOfWork.Object, Mock.Of<IMapper>(),
            new ECommerce.Application.Common.Fraud.FraudGate(new ECommerce.Application.Common.Fraud.FraudDetectionSettings()));
    }

    private static Order CreateOrderWithSeller(Guid sellerId)
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Satıcı ürünü" };
        product.AssignSeller(sellerId, autoApprove: true);
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-TEST" };
        order.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Quantity = 1,
            UnitPrice = 50
        });
        return order;
    }
}
