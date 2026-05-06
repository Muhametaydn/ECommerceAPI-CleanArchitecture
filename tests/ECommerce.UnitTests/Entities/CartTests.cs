using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Entities;

public class CartTests
{
    private Cart CreateCart() => new() { Id = "cart:test-user" };

    [Fact]
    public void AddItem_NewProduct_ShouldAddToItems()
    {
        var cart = CreateCart();
        cart.AddItem(Guid.NewGuid(), "iPhone 15", 55000m, 1);

        cart.Items.Should().HaveCount(1);
        cart.TotalPrice.Should().Be(55000m);
        cart.TotalItems.Should().Be(1);
    }

    [Fact]
    public void AddItem_ExistingProduct_ShouldIncreaseQuantity()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();

        cart.AddItem(productId, "iPhone 15", 55000m, 1);
        cart.AddItem(productId, "iPhone 15", 55000m, 2);

        cart.Items.Should().HaveCount(1);
        cart.Items.First().Quantity.Should().Be(3);
        cart.TotalPrice.Should().Be(165000m);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveFromCart()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "Test", 100m, 1);

        cart.RemoveItem(productId);

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void UpdateQuantity_WithPositive_ShouldUpdate()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "Test", 100m, 1);

        cart.UpdateQuantity(productId, 5);

        cart.Items.First().Quantity.Should().Be(5);
        cart.TotalPrice.Should().Be(500m);
    }

    [Fact]
    public void UpdateQuantity_WithZero_ShouldRemoveItem()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "Test", 100m, 1);

        cart.UpdateQuantity(productId, 0);

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        var cart = CreateCart();
        cart.AddItem(Guid.NewGuid(), "A", 100m, 1);
        cart.AddItem(Guid.NewGuid(), "B", 200m, 2);

        cart.Clear();

        cart.Items.Should().BeEmpty();
        cart.TotalPrice.Should().Be(0);
        cart.TotalItems.Should().Be(0);
    }

    [Fact]
    public void TotalPrice_ShouldSumAllSubTotals()
    {
        var cart = CreateCart();
        cart.AddItem(Guid.NewGuid(), "A", 100m, 2);   // 200
        cart.AddItem(Guid.NewGuid(), "B", 50m, 3);    // 150

        cart.TotalPrice.Should().Be(350m);
        cart.TotalItems.Should().Be(5);
    }

    [Fact]
    public void CartItem_SubTotal_ShouldBeCorrect()
    {
        var item = new CartItem { UnitPrice = 150m, Quantity = 3 };
        item.SubTotal.Should().Be(450m);
    }
}
