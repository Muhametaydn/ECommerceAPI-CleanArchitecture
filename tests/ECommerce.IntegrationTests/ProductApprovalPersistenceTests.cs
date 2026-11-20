using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests;

public class ProductApprovalPersistenceTests
{
    [Theory]
    [InlineData(ProductApprovalStatus.Pending)]
    [InlineData(ProductApprovalStatus.Approved)]
    [InlineData(ProductApprovalStatus.Rejected)]
    public async Task New_product_preserves_explicit_approval_after_relational_insert(ProductApprovalStatus status)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var seller = ApplicationUser.Create("Test", "Seller", "seller@example.invalid", "test-seller");
        db.Set<ApplicationUser>().Add(seller);
        var product = new Product
        {
            Id = Guid.NewGuid(), Name = "Approval regression", SKU = Guid.NewGuid().ToString(),
            Price = 100, StockQuantity = 10,
            CategoryId = Guid.Parse("a1111111-1111-1111-1111-111111111111")
        };
        product.AssignSeller(seller.Id, status == ProductApprovalStatus.Approved);
        if (status == ProductApprovalStatus.Rejected) product.Reject();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var reloaded = await db.Products.SingleAsync(value => value.Id == product.Id);
        Assert.Equal(status, reloaded.ApprovalStatus);
        Assert.Equal(status == ProductApprovalStatus.Approved, reloaded.IsActive);
    }
}