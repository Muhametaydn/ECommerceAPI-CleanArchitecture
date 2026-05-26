using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Application.Features.Products.Queries.SearchProducts;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Products;

/// <summary>
/// SearchProductsQueryHandler testleri.
///
/// Handler sorumluluğu:
/// 1. Query parametrelerini eksiksiz ISearchService.SearchProductsAsync'e iletmek.
/// 2. Dönen ProductSearchResult'u olduğu gibi çağrıcıya döndürmek.
/// 3. Boş sonuç için de sorunsuz çalışmak.
///
/// Elasticsearch bağlantısı test edilmez — ISearchService mock'lanır.
/// </summary>
public class SearchProductsQueryHandlerTests
{
    private readonly Mock<ISearchService> _searchServiceMock;
    private readonly SearchProductsQueryHandler _handler;

    public SearchProductsQueryHandlerTests()
    {
        _searchServiceMock = new Mock<ISearchService>();
        _handler = new SearchProductsQueryHandler(_searchServiceMock.Object);
    }

    // ── Temel Çalışma ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldCallSearchServiceWithAllQueryParameters()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var query = new SearchProductsQuery(
            Query: "laptop",
            CategoryId: categoryId,
            MinPrice: 1000m,
            MaxPrice: 5000m,
            InStock: true,
            SortBy: "price",
            SortDescending: true,
            PageNumber: 2,
            PageSize: 20);

        _searchServiceMock
            .Setup(s => s.SearchProductsAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSearchResult());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert — tüm parametreler servise iletilmeli
        _searchServiceMock.Verify(s => s.SearchProductsAsync(
            "laptop",
            categoryId,
            1000m,
            5000m,
            true,
            "price",
            true,
            2,
            20,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnResultFromSearchService()
    {
        // Arrange
        var expectedResult = new ProductSearchResult
        {
            Items = new List<ProductDTO>
            {
                new() { Id = Guid.NewGuid(), Name = "MacBook Pro", Price = 45000m },
                new() { Id = Guid.NewGuid(), Name = "Dell XPS", Price = 32000m }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _searchServiceMock
            .Setup(s => s.SearchProductsAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new SearchProductsQuery("laptop");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResult);
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    // ── Boş Sonuç ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoResultsFound_ShouldReturnEmptyProductSearchResult()
    {
        // Arrange
        var emptyResult = new ProductSearchResult
        {
            Items = [],
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _searchServiceMock
            .Setup(s => s.SearchProductsAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResult);

        var query = new SearchProductsQuery("bulunamaz-urun");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    // ── Null / Opsiyonel Parametreler ─────────────────────────────────────

    [Fact]
    public async Task Handle_WithOnlySearchTerm_ShouldPassNullsForOptionalParameters()
    {
        // Arrange
        _searchServiceMock
            .Setup(s => s.SearchProductsAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSearchResult());

        var query = new SearchProductsQuery("telefon");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert — opsiyonel parametreler null olarak iletilmeli
        _searchServiceMock.Verify(s => s.SearchProductsAsync(
            "telefon",
            null,   // CategoryId
            null,   // MinPrice
            null,   // MaxPrice
            null,   // InStock
            null,   // SortBy
            false,  // SortDescending (default)
            1,      // PageNumber (default)
            10,     // PageSize (default)
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullSearchQuery_ShouldStillCallSearchService()
    {
        // Arrange
        _searchServiceMock
            .Setup(s => s.SearchProductsAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSearchResult());

        var query = new SearchProductsQuery(Query: null); // tüm ürünler

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _searchServiceMock.Verify(s => s.SearchProductsAsync(
            null, null, null, null, null, null, false, 1, 10,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Sayfalama Bilgisi ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProductSearchResult_ShouldCalculatePaginationCorrectly()
    {
        // Arrange
        var result = new ProductSearchResult
        {
            Items = new List<ProductDTO>(Enumerable.Range(1, 10)
                .Select(i => new ProductDTO { Id = Guid.NewGuid(), Name = $"Ürün {i}" })),
            TotalCount = 55,
            PageNumber = 3,
            PageSize = 10
        };

        _searchServiceMock
            .Setup(s => s.SearchProductsAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var query = new SearchProductsQuery(null, PageNumber: 3, PageSize: 10);

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        response.TotalPages.Should().Be(6);      // ceil(55/10)
        response.HasPreviousPage.Should().BeTrue();
        response.HasNextPage.Should().BeTrue();
        response.PageNumber.Should().Be(3);
    }

    // ── CancellationToken ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToSearchService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        CancellationToken capturedToken = default;

        _searchServiceMock
            .Setup(s => s.SearchProductsAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<string?, Guid?, decimal?, decimal?, bool?, string?, bool, int, int, CancellationToken>(
                (_, _, _, _, _, _, _, _, _, ct) => capturedToken = ct)
            .ReturnsAsync(new ProductSearchResult());

        var query = new SearchProductsQuery("test");

        // Act
        await _handler.Handle(query, token);

        // Assert
        capturedToken.Should().Be(token);
    }
}
