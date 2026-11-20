using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ECommerce.API.Controllers;
using ECommerce.API.Middlewares;
using ECommerce.API.Services;
using ECommerce.Application;
using ECommerce.Application.Contracts.Identity;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure;
using ECommerce.Persistence;
using ECommerce.Persistence.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace ECommerce.IntegrationTests;

// Real controllers, JWT validation, policies, MediatR, validators and repositories.
// EF InMemory and no-op external services keep every test independent of live data.
public sealed class AuthorizationHttpTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly string _secret = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    private readonly Guid _seller = Guid.NewGuid();
    private readonly Guid _otherSeller = Guid.NewGuid();
    private readonly Guid _customer = Guid.NewGuid();
    private readonly Guid _category = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    private readonly Guid _product = Guid.NewGuid();
    private readonly Guid _otherProduct = Guid.NewGuid();
    private readonly Guid _order = Guid.NewGuid();
    private readonly Guid _item = Guid.NewGuid();
    private readonly Guid _otherItem = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = _secret,
            ["JwtSettings:Issuer"] = "security-tests",
            ["JwtSettings:Audience"] = "security-tests",
            ["EmailSettings:From"] = "tests@example.invalid",
            ["RabbitMQ:Host"] = "localhost"
        });
        builder.Services.AddApplicationServices();
        builder.Services.AddPersistenceServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);
        // AuthController needs this service even when JSON binding rejects the request before the action runs.
        builder.Services.RemoveAll<IAuthService>();
        builder.Services.AddSingleton(Mock.Of<IAuthService>());
        builder.Services.RemoveAll<IHostedService>();
        builder.Services.RemoveAll<IDomainEventDispatcher>();
        var databaseName = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        var cartService = new Mock<ICartService>();
        cartService.Setup(value => value.GetCartAsync(It.IsAny<string>())).ReturnsAsync(new Cart
        {
            Id = "checkout-test", Items = [new CartItem { ProductId = _product, ProductName = "Own product", Quantity = 1, UnitPrice = 100 }]
        });
        builder.Services.AddSingleton(cartService.Object);
        builder.Services.AddSingleton(Mock.Of<ICacheService>());
        builder.Services.AddSingleton(Mock.Of<ISearchService>());
        builder.Services.AddSingleton(Mock.Of<IProductImageStorage>());
        builder.Services.AddMarketplaceAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(ProductsController).Assembly);
        _app = builder.Build();
        _app.UseMiddleware<GlobalExceptionMiddleware>();
        _app.UseRouting();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();
        await _app.StartAsync();
        _client = _app.GetTestClient();
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        var first = new Product { Id = _product, Name = "Own product", Price = 100, SKU = "OWN", StockQuantity = 10, CategoryId = _category };
        first.AssignSeller(_seller, true);
        var second = new Product { Id = _otherProduct, Name = "Other product", Price = 200, SKU = "OTHER", StockQuantity = 10, CategoryId = _category };
        second.AssignSeller(_otherSeller, true);
        var address = new Address { Id = Guid.NewGuid(), UserId = _customer, AddressLine = "Test address", City = "Istanbul", District = "Test" };
        var order = new Order { Id = _order, OrderNumber = "TEST-MIXED", UserId = _customer, ShippingAddress = address, ShippingAddressId = address.Id };
        order.OrderItems.Add(new OrderItem { Id = _item, OrderId = _order, Product = first, ProductId = first.Id, Quantity = 2, UnitPrice = 100 });
        order.OrderItems.Add(new OrderItem { Id = _otherItem, OrderId = _order, Product = second, ProductId = second.Id, Quantity = 1, UnitPrice = 200 });
        order.CalculateTotal();
        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Theory]
    [InlineData("invalid-cart")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("a1111111-1111-1111-1111-111111111111,another")]
    public async Task AnonymousCart_InvalidHeaderReturnsBadRequest(string cartId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cart");
        request.Headers.Add("X-Cart-Id", cartId);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousCart_ValidHeaderIsBoundSuccessfully()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cart");
        request.Headers.Add("X-Cart-Id", Guid.NewGuid().ToString());

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/cart/items", "{\"quantity\":1}")]
    [InlineData("POST", "/api/v1/cart/items", "{\"productId\":\"a1111111-1111-1111-1111-111111111111\"}")]
    [InlineData("PUT", "/api/v1/cart/items/a1111111-1111-1111-1111-111111111111", "{}")]
    [InlineData("POST", "/api/v1/products/a1111111-1111-1111-1111-111111111111/reviews", "{}")]
    [InlineData("PUT", "/api/v1/reviews/a1111111-1111-1111-1111-111111111111", "{}")]
    [InlineData("POST", "/api/v1/auth/confirm-email-change", "{\"newEmail\":\"person@example.com\",\"token\":\"test\"}")]
    public async Task MissingRequiredValue_IsRejectedDuringJsonBinding(string method, string path, string json)
    {
        Authenticate("Customer", _customer);
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("errors", out var errors),
            $"Expected JSON binding errors; received: {body.RootElement}");
        Assert.True(errors.TryGetProperty("$", out _),
            $"Expected a JSON binding error at '$'; received: {body.RootElement}");
    }

    private void Authenticate(string role, Guid? id = null, string? invalid = null)
    {
        var token = new JwtSecurityToken(
            issuer: invalid == "issuer" ? "wrong-issuer" : "security-tests",
            audience: "security-tests",
            claims: [new Claim(ClaimTypes.NameIdentifier, (id ?? _seller).ToString()), new Claim(ClaimTypes.Role, role)],
            expires: invalid == "expired" ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(invalid == "signature" ? new string('x', 64) : _secret)), SecurityAlgorithms.HmacSha256));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    [Theory]
    [InlineData("products/admin")]
    [InlineData("products/mine")]
    [InlineData("orders/admin")]
    [InlineData("orders/seller")]
    public async Task Anonymous_is_challenged(string endpoint)
        => Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/" + endpoint)).StatusCode);

    [Theory]
    [InlineData("signature")]
    [InlineData("expired")]
    [InlineData("issuer")]
    public async Task Invalid_token_cannot_gain_admin_access(string invalid)
    {
        Authenticate("Admin", invalid: invalid);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/products/admin")).StatusCode);
    }

    [Theory]
    [InlineData("Customer", "products/admin")]
    [InlineData("Seller", "products/admin")]
    [InlineData("Customer", "products/mine")]
    [InlineData("Customer", "orders/admin")]
    [InlineData("Seller", "orders/admin")]
    [InlineData("Customer", "orders/seller")]
    public async Task Authenticated_wrong_role_is_forbidden(string role, string endpoint)
    {
        Authenticate(role);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/v1/" + endpoint)).StatusCode);
    }

    [Theory]
    [InlineData("products/admin")]
    [InlineData("orders/admin")]
    public async Task Admin_can_read_admin_routes(string endpoint)
    {
        Authenticate("Admin");
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/" + endpoint)).StatusCode);
    }

    [Fact]
    public async Task Seller_product_list_excludes_other_sellers()
    {
        Authenticate("Seller");
        var response = await _client.GetAsync("/api/v1/products/mine");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(_product, Assert.Single(rows.EnumerateArray()).GetProperty("id").GetGuid());
    }

    private object ProductUpdate(Guid requester, bool admin = false) => new
    {
        id = _product, name = "Changed", description = "Test", price = 150,
        stockQuantity = 10, sku = "UPDATED", categoryId = _category, isActive = true,
        requesterId = requester, isAdmin = admin
    };

    [Fact]
    public async Task Forged_body_cannot_edit_another_sellers_product()
    {
        Authenticate("Seller");
        var response = await _client.PutAsJsonAsync($"/api/v1/products/{_otherProduct}", ProductUpdate(_otherSeller, true));
        await AssertForbiddenProblem(response);
        using var scope = _app.Services.CreateScope();
        var product = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Products.FindAsync(_otherProduct);
        Assert.Equal("Other product", product!.Name);
        Assert.Equal(200, product.Price);
        Assert.Equal(ProductApprovalStatus.Approved, product.ApprovalStatus);
    }

    [Theory]
    [InlineData("Seller")]
    [InlineData("Admin")]
    public async Task Authorized_product_update_uses_token_identity(string role)
    {
        Authenticate(role);
        var response = await _client.PutAsJsonAsync($"/api/v1/products/{_product}", ProductUpdate(_otherSeller, true));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _app.Services.CreateScope();
        var product = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Products.FindAsync(_product);
        Assert.Equal("Changed", product!.Name);
        Assert.Equal(role == "Seller" ? ProductApprovalStatus.Pending : ProductApprovalStatus.Approved, product.ApprovalStatus);
    }

    [Fact]
    public async Task Mixed_order_list_reveals_only_own_items_and_subtotal()
    {
        Authenticate("Seller");
        var response = await _client.GetAsync("/api/v1/orders/seller");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = await response.Content.ReadFromJsonAsync<JsonElement>();
        var order = Assert.Single(rows.EnumerateArray());
        Assert.True(order.GetProperty("containsOtherSellers").GetBoolean());
        Assert.Equal(200, order.GetProperty("sellerSubtotal").GetDecimal());
        Assert.Equal(_item, Assert.Single(order.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Seller_cannot_change_another_item_even_with_forged_identity()
    {
        Authenticate("Seller");
        var response = await _client.PatchAsJsonAsync($"/api/v1/orders/{_order}/items/{_otherItem}/status", new { action = "prepare", requesterId = _otherSeller, isAdmin = true });
        await AssertForbiddenProblem(response);
        await AssertItemStates(OrderItemStatus.Pending, OrderItemStatus.Pending);
    }

    [Fact]
    public async Task Seller_can_progress_own_item_without_changing_other_item()
    {
        Authenticate("Seller");
        foreach (var action in new[] { "prepare", "ship", "deliver" })
            Assert.Equal(HttpStatusCode.OK, (await _client.PatchAsJsonAsync($"/api/v1/orders/{_order}/items/{_item}/status", new { action })).StatusCode);
        await AssertItemStates(OrderItemStatus.Delivered, OrderItemStatus.Pending);
    }

    [Fact]
    public async Task Seller_cannot_progress_entire_mixed_order()
    {
        Authenticate("Seller");
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PatchAsJsonAsync($"/api/v1/orders/{_order}/status", new { action = "confirm" })).StatusCode);
        await AssertItemStates(OrderItemStatus.Pending, OrderItemStatus.Pending);
    }

    [Theory]
    [InlineData("detail")]
    [InlineData("cancel")]
    public async Task Customer_cannot_read_or_cancel_another_customers_order(string operation)
    {
        Authenticate("Customer", Guid.NewGuid());
        var response = operation == "detail"
            ? await _client.GetAsync($"/api/v1/orders/{_order}")
            : await _client.PostAsync($"/api/v1/orders/{_order}/cancel", null);
        await AssertForbiddenProblem(response);
        await AssertItemStates(OrderItemStatus.Pending, OrderItemStatus.Pending);
    }

    [Fact]
    public async Task Checkout_cannot_use_another_customers_address_even_with_forged_user_id()
    {
        Guid addressId;
        using (var scope = _app.Services.CreateScope())
            addressId = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Address.Select(value => value.Id).SingleAsync();
        Authenticate("Customer", Guid.NewGuid());
        var response = await _client.PostAsJsonAsync("/api/v1/orders", new { checkoutRequestId = Guid.NewGuid(), shippingAddressId = addressId, paymentMethod = 2, userId = _customer });
        await AssertForbiddenProblem(response);
        using var verification = _app.Services.CreateScope();
        var db = verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(10, (await db.Products.FindAsync(_product))!.StockQuantity);
    }

    private async Task AssertItemStates(OrderItemStatus own, OrderItemStatus other)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(own, (await db.OrderItems.FindAsync(_item))!.Status);
        Assert.Equal(other, (await db.OrderItems.FindAsync(_otherItem))!.Status);
    }

    private static async Task AssertForbiddenProblem(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(403, problem.GetProperty("status").GetInt32());
    }
}
