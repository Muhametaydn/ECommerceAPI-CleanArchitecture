using System.Linq.Expressions;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Products.Commands.DeleteProduct;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Products;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<ISearchService> _search = new();
    private readonly Mock<IProductImageStorage> _images = new();
    private readonly Product _product = new()
    {
        Id = Guid.NewGuid(),
        Name = "Mouse",
        IsActive = true
    };

    public DeleteProductCommandHandlerTests()
    {
        _product.UpdateImage("/uploads/products/abc.png");
        _unitOfWork.SetupGet(u => u.Order).Returns(_orders.Object);
        _products.Setup(p => p.GetByIdAsync(_product.Id)).ReturnsAsync(_product);
    }

    [Fact]
    public async Task Product_that_appears_in_orders_is_deactivated_not_deleted()
    {
        // Regression: FK Cascade nedeniyle ürün silmek tüm geçmiş sipariş kalemlerini siliyordu.
        OrdersReferenceProduct(true);

        await Handler().Handle(new DeleteProductCommand(_product.Id), CancellationToken.None);

        _product.IsActive.Should().BeFalse();
        _products.Verify(p => p.Delete(It.IsAny<Product>()), Times.Never);
        _products.Verify(p => p.Update(_product), Times.Once);
        _search.Verify(s => s.DeleteProductFromIndexAsync(_product.Id, It.IsAny<CancellationToken>()), Times.Once);
        _images.Verify(i => i.DeleteIfUnreferencedAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        _images.Verify(i => i.DeleteIfUnreferencedAsync(_product.ImageUrl, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Never_ordered_product_is_deleted_with_its_image()
    {
        OrdersReferenceProduct(false);

        await Handler().Handle(new DeleteProductCommand(_product.Id), CancellationToken.None);

        _products.Verify(p => p.Delete(_product), Times.Once);
        _images.Verify(i => i.DeleteIfUnreferencedAsync("/uploads/products/abc.png", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveByPrefixAsync("products:list:", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync($"products:single:{_product.Id}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Missing_product_throws_not_found()
    {
        var act = () => Handler().Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    private void OrdersReferenceProduct(bool referenced)
        => _orders.Setup(o => o.AnyAsync(It.IsAny<Expression<Func<Order, bool>>>())).ReturnsAsync(referenced);

    private DeleteProductCommandHandler Handler()
        => new(_products.Object, _unitOfWork.Object, _cache.Object, _search.Object, _images.Object);
}
