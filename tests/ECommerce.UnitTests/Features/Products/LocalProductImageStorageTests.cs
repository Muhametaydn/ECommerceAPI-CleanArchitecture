using ECommerce.API.Services;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.UnitTests.Features.Products;

public sealed class LocalProductImageStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ecommerce-image-storage-{Guid.NewGuid():N}");
    private readonly ApplicationDbContext _dbContext;
    private readonly LocalProductImageStorage _storage;

    public LocalProductImageStorageTests()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.WebRootPath).Returns(_root);
        environment.SetupGet(value => value.ContentRootPath).Returns(_root);
        _storage = new LocalProductImageStorage(
            environment.Object,
            _dbContext,
            Mock.Of<ILogger<LocalProductImageStorage>>());
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateManagedFile()
    {
        await using var content = new MemoryStream([1, 2, 3]);

        var url = await _storage.SaveAsync(content, ".png");

        url.Should().MatchRegex("^/uploads/products/[a-f0-9]{32}\\.png$");
        File.Exists(GetPath(url)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteIfUnreferencedAsync_ShouldKeepReferencedFile_AndDeleteOrphan()
    {
        await using var referencedContent = new MemoryStream([1]);
        var referencedUrl = await _storage.SaveAsync(referencedContent, ".jpg");
        await using var orphanContent = new MemoryStream([2]);
        var orphanUrl = await _storage.SaveAsync(orphanContent, ".webp");

        var product = new Product { Id = Guid.NewGuid(), Name = "Ürün", SKU = "TEST" };
        product.UpdateImage(referencedUrl);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        await _storage.DeleteIfUnreferencedAsync(referencedUrl);
        await _storage.DeleteIfUnreferencedAsync(orphanUrl);

        File.Exists(GetPath(referencedUrl)).Should().BeTrue();
        File.Exists(GetPath(orphanUrl)).Should().BeFalse();
    }

    [Fact]
    public async Task CleanupOrphansAsync_ShouldRespectGracePeriod()
    {
        await using var oldContent = new MemoryStream([1]);
        var oldUrl = await _storage.SaveAsync(oldContent, ".png");
        File.SetLastWriteTimeUtc(GetPath(oldUrl), DateTime.UtcNow.AddDays(-2));
        await using var newContent = new MemoryStream([2]);
        var newUrl = await _storage.SaveAsync(newContent, ".png");

        var deletedCount = await _storage.CleanupOrphansAsync(TimeSpan.FromHours(24));

        deletedCount.Should().Be(1);
        File.Exists(GetPath(oldUrl)).Should().BeFalse();
        File.Exists(GetPath(newUrl)).Should().BeTrue();
    }

    private string GetPath(string url)
        => Path.Combine(_root, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    public void Dispose()
    {
        _dbContext.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
