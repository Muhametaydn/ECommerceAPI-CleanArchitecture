using AutoMapper;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Application.Features.Products.Mappings;
using ECommerce.Application.Features.Products.Queries.GetAllProducts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.Specifications;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ECommerce.UnitTests.Features.Products;

/// <summary>
/// GetAllProductsQueryHandler testleri.
///
/// Handler'in sorumlulugu:
/// 1. Query parametrelerinden dogru Specification olusturmak
/// 2. Repository'yi Specification ile cagirmak
/// 3. Sonucu PaginatedResult olarak dondurmek
///
/// Bu testlerde repository mock'lanir — DB'ye gidilmez.
/// Specification'in kendisi ayri test sinifinda test edilir.
/// </summary>
public class GetAllProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly IMapper _mapper;
    private readonly GetAllProductsQueryHandler _handler;

    public GetAllProductsQueryHandlerTests()
    {
        _productRepoMock = new Mock<IProductRepository>();

        // Gercek AutoMapper profili kullan — mapping hatalarini da yakalariz
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<ProductMappingProfile>(),
            NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _handler = new GetAllProductsQueryHandler(_productRepoMock.Object, _mapper);
    }

    private static List<Product> GetSampleProducts() => new()
    {
        new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Urun 1",
            Description = "Aciklama 1",
            Price = 100m,
            StockQuantity = 10,
            SKU = "TST-001",
            IsActive = true,
            CategoryId = Guid.NewGuid(),
            Category = new Category { Name = "Elektronik" }
        },
        new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Urun 2",
            Description = "Aciklama 2",
            Price = 200m,
            StockQuantity = 5,
            SKU = "TST-002",
            IsActive = true,
            CategoryId = Guid.NewGuid(),
            Category = new Category { Name = "Aksesuar" }
        }
    };

    // ── TEMEL CALISMA ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithDefaultQuery_ShouldReturnPaginatedResult()
    {
        // Arrange
        var products = GetSampleProducts();
        _productRepoMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(products);
        _productRepoMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(2);

        var query = new GetAllProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<PaginatedResult<ProductDTO>>();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithSpecification()
    {
        // Arrange
        _productRepoMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(new List<Product>());
        _productRepoMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(0);

        var query = new GetAllProductsQuery
        {
            SearchTerm = "laptop",
            MinPrice = 1000,
            MaxPrice = 5000
        };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert — Repository'nin ListAsync ve CountAsync'i Specification ile cagirilmis olmali
        _productRepoMock.Verify(
            r => r.ListAsync(It.IsAny<ISpecification<Product>>()),
            Times.Once);
        _productRepoMock.Verify(
            r => r.CountAsync(It.IsAny<ISpecification<Product>>()),
            Times.Once);
    }

    // ── MAPPING ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldMapProductsToDTO()
    {
        // Arrange
        var products = GetSampleProducts();
        _productRepoMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(products);
        _productRepoMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(2);

        var query = new GetAllProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert — DTO'daki alanlar entity'den dogru map edilmis olmali
        var dto = result.Items.First();
        dto.Name.Should().Be("Test Urun 1");
        dto.Price.Should().Be(100m);
        dto.CategoryName.Should().Be("Elektronik");
    }

    // ── SAYFALAMA ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithPagination_ShouldSetCorrectPageInfo()
    {
        // Arrange
        _productRepoMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(GetSampleProducts());
        _productRepoMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(50); // toplam 50 urun var

        var query = new GetAllProductsQuery
        {
            PageNumber = 3,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(50);
        result.TotalPages.Should().Be(5);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    // ── BOS SONUC ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoProductsFound_ShouldReturnEmptyResult()
    {
        // Arrange
        _productRepoMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(new List<Product>());
        _productRepoMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(0);

        var query = new GetAllProductsQuery { SearchTerm = "bulunamaz" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
    }

    // ── CURSOR ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithCursor_ShouldCallRepositoryWithSpec()
    {
        // Arrange
        _productRepoMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(new List<Product>());
        _productRepoMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(0);

        var cursorId = Guid.NewGuid();
        var query = new GetAllProductsQuery
        {
            Cursor = cursorId.ToString(),
            PageSize = 20
        };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert — cursor gonderildiginde de repository cagrilmali
        _productRepoMock.Verify(
            r => r.ListAsync(It.IsAny<ISpecification<Product>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCursor_ShouldFallbackToOffsetPagination()
    {
        // Arrange
        _productRepoMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(GetSampleProducts());
        _productRepoMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Product>>()))
            .ReturnsAsync(2);

        var query = new GetAllProductsQuery
        {
            Cursor = "gecersiz-cursor-degeri",  // Guid parse edilemez
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert — hata vermemeli, offset-based'e dusmeli
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
    }
}
