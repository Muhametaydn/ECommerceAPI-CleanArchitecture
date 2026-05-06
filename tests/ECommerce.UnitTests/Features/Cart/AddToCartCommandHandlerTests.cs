using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Cart.Commands.AddToCart;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Cart;

public class AddToCartCommandHandlerTests
{
    private readonly Mock<ICartService> _cartServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly AddToCartCommandHandler _handler;

    public AddToCartCommandHandlerTests()
    {
        _cartServiceMock = new Mock<ICartService>();
        _productRepoMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Product).Returns(_productRepoMock.Object);

        _handler = new AddToCartCommandHandler(_cartServiceMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidProduct_ShouldAddToCart()
    {
        var productId = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new Product { Id = productId, Name = "Test", Price = 100m, StockQuantity = 10, IsActive = true });
        _cartServiceMock.Setup(s => s.GetCartAsync(It.IsAny<string>()))
            .ReturnsAsync((Domain.Entities.Cart?)null);

        var command = new AddToCartCommand { ProductId = productId, Quantity = 2, CartId = "cart:test" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalItems.Should().Be(2);
        result.TotalPrice.Should().Be(200m);
        _cartServiceMock.Verify(s => s.SaveCartAsync(It.IsAny<Domain.Entities.Cart>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldThrowNotFoundException()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        var command = new AddToCartCommand { ProductId = Guid.NewGuid(), Quantity = 1, CartId = "cart:test" };

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InactiveProduct_ShouldThrow()
    {
        var productId = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new Product { Id = productId, IsActive = false, StockQuantity = 10 });

        var command = new AddToCartCommand { ProductId = productId, Quantity = 1, CartId = "cart:test" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InsufficientStock_ShouldThrow()
    {
        var productId = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new Product { Id = productId, Name = "Test", StockQuantity = 3, IsActive = true });
        _cartServiceMock.Setup(s => s.GetCartAsync(It.IsAny<string>()))
            .ReturnsAsync((Domain.Entities.Cart?)null);

        var command = new AddToCartCommand { ProductId = productId, Quantity = 5, CartId = "cart:test" };

        var act = () => _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Yetersiz stok*");
    }

    [Fact]
    public async Task Handle_ExistingCartItem_ShouldCheckTotalQuantityAgainstStock()
    {
        var productId = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new Product { Id = productId, Name = "Test", Price = 100m, StockQuantity = 5, IsActive = true });

        var existingCart = new Domain.Entities.Cart { Id = "cart:test" };
        existingCart.AddItem(productId, "Test", 100m, 3); // zaten 3 tane var

        _cartServiceMock.Setup(s => s.GetCartAsync("cart:test")).ReturnsAsync(existingCart);

        // 3 + 3 = 6 > stok(5) — hata vermeli
        var command = new AddToCartCommand { ProductId = productId, Quantity = 3, CartId = "cart:test" };

        var act = () => _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
