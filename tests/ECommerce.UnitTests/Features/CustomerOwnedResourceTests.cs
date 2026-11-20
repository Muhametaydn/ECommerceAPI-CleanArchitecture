using System.Linq.Expressions;
using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Addresses.Commands.CreateAddress;
using ECommerce.Application.Features.Addresses.Commands.DeleteAddress;
using ECommerce.Application.Features.Addresses.Commands.UpdateAddress;
using ECommerce.Application.Features.Cart.Commands.UpdateCartItem;
using ECommerce.Application.Features.Coupons.Queries.ValidateCoupon;
using ECommerce.Application.Features.Reviews.Commands.CreateReview;
using ECommerce.Application.Features.Reviews.Commands.DeleteReview;
using ECommerce.Application.Features.Reviews.Commands.UpdateReview;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features;

// Kullanıcıya ait kaynaklarda sahiplik (IDOR) ve iş kuralı kontrolleri.
public class CustomerOwnedResourceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IReviewRepository> _reviews = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IAddressRepository> _addresses = new();
    private readonly Mock<ICouponRepository> _coupons = new();
    private readonly IMapper _mapper = Mock.Of<IMapper>();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _intruder = Guid.NewGuid();
    private readonly Product _product = new() { Id = Guid.NewGuid(), Name = "Lamba", Price = 50, StockQuantity = 3, IsActive = true };

    public CustomerOwnedResourceTests()
    {
        _uow.SetupGet(u => u.Review).Returns(_reviews.Object);
        _uow.SetupGet(u => u.Order).Returns(_orders.Object);
        _uow.SetupGet(u => u.Product).Returns(_products.Object);
        _uow.SetupGet(u => u.Address).Returns(_addresses.Object);
        _uow.SetupGet(u => u.Coupon).Returns(_coupons.Object);
        _products.Setup(p => p.GetByIdAsync(_product.Id)).ReturnsAsync(_product);
    }

    // ── Reviews ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Review_requires_a_delivered_purchase_of_the_product()
    {
        PurchaseHistory(OrderStatus.Shipped);

        var act = () => CreateReview();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*satın aldığınız*");
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task Review_is_saved_for_delivered_purchase()
    {
        PurchaseHistory(OrderStatus.Delivered);

        await CreateReview(rating: 4);

        _reviews.Verify(r => r.AddAsync(It.Is<Review>(v => v.UserId == _owner && v.ProductId == _product.Id && v.Rating == 4)), Times.Once);
    }

    [Fact]
    public async Task Second_review_for_same_product_is_rejected()
    {
        PurchaseHistory(OrderStatus.Delivered);
        _reviews.Setup(r => r.HasUserReviewedProductAsync(_owner, _product.Id)).ReturnsAsync(true);

        var act = () => CreateReview();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*zaten*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Review_rating_outside_1_to_5_is_rejected(int rating)
    {
        PurchaseHistory(OrderStatus.Delivered);

        var act = () => CreateReview(rating);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Only_the_author_can_edit_a_review()
    {
        var review = ExistingReview();

        var act = () => new UpdateReviewCommandHandler(_uow.Object, _mapper).Handle(
            new UpdateReviewCommand { Id = review.Id, UserId = _intruder, Title = "hack", Comment = "x", Rating = 1 }, default);

        await act.Should().ThrowAsync<ForbiddenException>();
        review.Title.Should().Be("Güzel");
    }

    [Fact]
    public async Task Author_or_admin_can_delete_a_review_but_nobody_else()
    {
        var review = ExistingReview();
        var handler = new DeleteReviewCommandHandler(_uow.Object);

        await handler.Invoking(h => h.Handle(new DeleteReviewCommand { Id = review.Id, UserId = _intruder }, default))
            .Should().ThrowAsync<ForbiddenException>();
        await handler.Handle(new DeleteReviewCommand { Id = review.Id, UserId = _intruder, IsAdmin = true }, default);
        await handler.Handle(new DeleteReviewCommand { Id = review.Id, UserId = _owner }, default);

        _reviews.Verify(r => r.Delete(review), Times.Exactly(2));
    }

    // ── Addresses ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Address_of_another_user_cannot_be_updated_or_deleted()
    {
        var address = ExistingAddress();

        await new UpdateAddressCommandHandler(_uow.Object, _mapper)
            .Invoking(h => h.Handle(new UpdateAddressCommand { Id = address.Id, UserId = _intruder, City = "Ankara" }, default))
            .Should().ThrowAsync<ForbiddenException>();
        await new DeleteAddressCommandHandler(_uow.Object)
            .Invoking(h => h.Handle(new DeleteAddressCommand { Id = address.Id, UserId = _intruder }, default))
            .Should().ThrowAsync<ForbiddenException>();

        address.City.Should().Be("İstanbul");
        _addresses.Verify(a => a.Delete(It.IsAny<Address>()), Times.Never);
    }

    [Fact]
    public async Task Address_used_by_an_active_order_cannot_be_deleted()
    {
        var address = ExistingAddress();
        _orders.Setup(o => o.AnyAsync(It.IsAny<Expression<Func<Order, bool>>>())).ReturnsAsync(true);

        var act = () => new DeleteAddressCommandHandler(_uow.Object)
            .Handle(new DeleteAddressCommand { Id = address.Id, UserId = _owner }, default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*aktif siparişlerde*");
    }

    [Fact]
    public async Task Owner_can_update_and_delete_own_address()
    {
        var address = ExistingAddress();

        await new UpdateAddressCommandHandler(_uow.Object, _mapper)
            .Handle(new UpdateAddressCommand { Id = address.Id, UserId = _owner, Title = "İş", AddressLine = "Yeni", City = "Ankara", District = "Çankaya", PostalCode = "06000" }, default);
        await new DeleteAddressCommandHandler(_uow.Object)
            .Handle(new DeleteAddressCommand { Id = address.Id, UserId = _owner }, default);

        address.City.Should().Be("Ankara");
        _addresses.Verify(a => a.Delete(address), Times.Once);
    }

    [Fact]
    public async Task Address_cannot_be_created_for_unknown_user()
    {
        var users = new Mock<IUserLookupService>();
        users.Setup(u => u.ExistsAsync(_owner, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => new CreateAddressCommandHandler(_uow.Object, _mapper, users.Object)
            .Handle(new CreateAddressCommand { UserId = _owner, City = "İzmir" }, default);

        await act.Should().ThrowAsync<NotFoundException>();
        _addresses.Verify(a => a.AddAsync(It.IsAny<Address>()), Times.Never);
    }

    // ── Coupon validation ────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateCoupon_reports_discount_and_final_amount()
    {
        _coupons.Setup(c => c.GetByCodeAsync("YUZDE15")).ReturnsAsync(new Coupon
        {
            Code = "YUZDE15", DiscountType = DiscountType.Percentage, DiscountValue = 15,
            MaxUsageCount = 5, ExpiryDate = DateTime.UtcNow.AddDays(1)
        });

        var result = await new ValidateCouponQueryHandler(_uow.Object)
            .Handle(new ValidateCouponQuery { Code = "yuzde15", OrderTotal = 199.99m }, default);

        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(30.00m);
        result.FinalAmount.Should().Be(169.99m);
    }

    [Fact]
    public async Task ValidateCoupon_rejects_unknown_expired_and_below_minimum()
    {
        _coupons.Setup(c => c.GetByCodeAsync("ESKI")).ReturnsAsync(new Coupon
        {
            Code = "ESKI", DiscountType = DiscountType.FixedAmount, DiscountValue = 10, MaxUsageCount = 5,
            ExpiryDate = DateTime.UtcNow.AddDays(-1)
        });
        _coupons.Setup(c => c.GetByCodeAsync("MIN500")).ReturnsAsync(new Coupon
        {
            Code = "MIN500", DiscountType = DiscountType.FixedAmount, DiscountValue = 10, MaxUsageCount = 5,
            MinimumOrderAmount = 500, ExpiryDate = DateTime.UtcNow.AddDays(1)
        });
        var handler = new ValidateCouponQueryHandler(_uow.Object);

        (await handler.Handle(new ValidateCouponQuery { Code = "YOK", OrderTotal = 100 }, default)).IsValid.Should().BeFalse();
        (await handler.Handle(new ValidateCouponQuery { Code = "ESKI", OrderTotal = 100 }, default)).IsValid.Should().BeFalse();
        (await handler.Handle(new ValidateCouponQuery { Code = "MIN500", OrderTotal = 499 }, default)).IsValid.Should().BeFalse();
    }

    // ── Cart quantity updates ────────────────────────────────────────────────

    [Fact]
    public async Task Cart_quantity_cannot_exceed_stock_or_be_raised_for_inactive_product()
    {
        var cartService = CartWith(1);
        var handler = new UpdateCartItemCommandHandler(cartService.Object, _uow.Object);

        await handler.Invoking(h => h.Handle(new UpdateCartItemCommand { CartId = "cart:x", ProductId = _product.Id, Quantity = 4 }, default))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*Yetersiz stok*");

        _product.IsActive = false;
        await handler.Invoking(h => h.Handle(new UpdateCartItemCommand { CartId = "cart:x", ProductId = _product.Id, Quantity = 2 }, default))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*satışta değil*");

        cartService.Verify(c => c.SaveCartAsync(It.IsAny<ECommerce.Domain.Entities.Cart>()), Times.Never);
    }

    [Fact]
    public async Task Cart_quantity_zero_removes_item_even_for_inactive_product()
    {
        _product.IsActive = false;
        var cartService = CartWith(2);

        var result = await new UpdateCartItemCommandHandler(cartService.Object, _uow.Object)
            .Handle(new UpdateCartItemCommand { CartId = "cart:x", ProductId = _product.Id, Quantity = 0 }, default);

        result.Items.Should().BeEmpty();
        cartService.Verify(c => c.SaveCartAsync(It.IsAny<ECommerce.Domain.Entities.Cart>()), Times.Once);
    }

    private Mock<ICartService> CartWith(int quantity)
    {
        var cart = new ECommerce.Domain.Entities.Cart { Id = "cart:x" };
        cart.AddItem(_product.Id, _product.Name, _product.Price, quantity);
        var cartService = new Mock<ICartService>();
        cartService.Setup(c => c.GetCartAsync("cart:x")).ReturnsAsync(cart);
        return cartService;
    }

    private Task<ECommerce.Application.Features.Reviews.DTOs.ReviewDTO> CreateReview(int rating = 5)
        => new CreateReviewCommandHandler(_uow.Object, _mapper).Handle(new CreateReviewCommand
        {
            UserId = _owner, ProductId = _product.Id, Title = "Başlık", Comment = "Yorum", Rating = rating
        }, default);

    private void PurchaseHistory(OrderStatus status)
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = _owner, Status = status };
        order.OrderItems.Add(new OrderItem { ProductId = _product.Id, Quantity = 1, UnitPrice = 50 });
        _orders.Setup(o => o.GetOrdersByUserAsync(_owner)).ReturnsAsync(new List<Order> { order });
    }

    private Review ExistingReview()
    {
        var review = new Review { Id = Guid.NewGuid(), UserId = _owner, ProductId = _product.Id, Title = "Güzel", Comment = "..." };
        review.SetRating(5);
        _reviews.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);
        return review;
    }

    private Address ExistingAddress()
    {
        var address = new Address { Id = Guid.NewGuid(), UserId = _owner, City = "İstanbul", AddressLine = "Eski", District = "Kadıköy" };
        _addresses.Setup(a => a.GetByIdAsync(address.Id)).ReturnsAsync(address);
        return address;
    }
}
