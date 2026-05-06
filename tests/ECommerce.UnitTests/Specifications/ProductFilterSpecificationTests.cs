using ECommerce.Application.Features.Products.Specifications;
using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Specifications;

/// <summary>
/// ProductFilterSpecification testleri.
///
/// Specification Pattern'in gucu burada ortaya cikiyor:
/// Filtreleme mantigi repository'den bagimsiz oldugundan,
/// veritabani olmadan pure in-memory test yapabiliyoruz.
///
/// Her test, Specification'in olusturdugu Criteria expression'ini
/// bir Product listesi uzerinde calistirip sonucu dogruluyor.
/// </summary>
public class ProductFilterSpecificationTests
{
    // Test verisi — tum testlerde kullanilacak ornek urunler
    private static List<Product> GetTestProducts() => new()
    {
        new Product
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "iPhone 15 Pro",
            Description = "Apple akilli telefon",
            Price = 55000m,
            StockQuantity = 25,
            SKU = "APL-IP15P",
            IsActive = true,
            CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        },
        new Product
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Name = "Samsung Galaxy S24",
            Description = "Samsung akilli telefon",
            Price = 42000m,
            StockQuantity = 30,
            SKU = "SMS-GS24",
            IsActive = true,
            CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        },
        new Product
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Name = "MacBook Air M3",
            Description = "Apple laptop bilgisayar",
            Price = 48000m,
            StockQuantity = 0,  // Stokta yok!
            SKU = "APL-MBA-M3",
            IsActive = true,
            CategoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        },
        new Product
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
            Name = "Pasif Urun",
            Description = "Bu urun artik satilmiyor",
            Price = 100m,
            StockQuantity = 50,
            SKU = "PASIF-001",
            IsActive = false,  // Pasif!
            CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        },
        new Product
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
            Name = "Logitech MX Master 3S",
            Description = "Kablosuz mouse",
            Price = 3500m,
            StockQuantity = 100,
            SKU = "LOG-MXM3S",
            IsActive = true,
            CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        }
    };

    /// <summary>
    /// Specification'in Criteria'sini bir liste uzerinde uygulayan helper.
    /// Expression<Func<T, bool>> -> Func<T, bool> cevirmesi (.Compile())
    /// </summary>
    private static List<Product> ApplySpec(ProductFilterSpecification spec, List<Product>? products = null)
    {
        products ??= GetTestProducts();
        var query = products.AsQueryable();

        if (spec.Criteria != null)
            query = query.Where(spec.Criteria);

        if (spec.OrderBy != null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending != null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.IsPagingEnabled)
        {
            if (spec.Skip.HasValue)
                query = query.Skip(spec.Skip.Value);
            if (spec.Take.HasValue)
                query = query.Take(spec.Take.Value);
        }

        return query.ToList();
    }

    // ── TEMEL FILTRELEME ─────────────────────────────────────────────────

    [Fact]
    public void DefaultSpec_ShouldOnlyReturnActiveProducts()
    {
        // Arrange — hicbir filtre yok
        var spec = new ProductFilterSpecification();

        // Act
        var result = ApplySpec(spec);

        // Assert — pasif urun (IsActive=false) gelmemeli
        result.Should().HaveCount(4);
        result.Should().NotContain(p => p.Name == "Pasif Urun");
        result.Should().OnlyContain(p => p.IsActive);
    }

    // ── ARAMA (SEARCH) ──────────────────────────────────────────────────

    [Fact]
    public void SearchTerm_ByName_ShouldFilterCorrectly()
    {
        // Arrange — "iphone" arasin (case-insensitive)
        var spec = new ProductFilterSpecification(searchTerm: "iphone");

        // Act
        var result = ApplySpec(spec);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("iPhone 15 Pro");
    }

    [Fact]
    public void SearchTerm_ByDescription_ShouldFilterCorrectly()
    {
        // Arrange — "laptop" aciklamada aransin
        var spec = new ProductFilterSpecification(searchTerm: "laptop");

        // Act
        var result = ApplySpec(spec);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("MacBook Air M3");
    }

    [Fact]
    public void SearchTerm_BySKU_ShouldFilterCorrectly()
    {
        // Arrange — SKU ile arama
        var spec = new ProductFilterSpecification(searchTerm: "LOG-MXM3S");

        // Act
        var result = ApplySpec(spec);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Logitech MX Master 3S");
    }

    [Fact]
    public void SearchTerm_CaseInsensitive_ShouldWork()
    {
        // Arrange — buyuk/kucuk harf farketmemeli
        var spec = new ProductFilterSpecification(searchTerm: "SAMSUNG");

        // Act
        var result = ApplySpec(spec);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Contain("Samsung");
    }

    [Fact]
    public void SearchTerm_NoMatch_ShouldReturnEmpty()
    {
        // Arrange
        var spec = new ProductFilterSpecification(searchTerm: "bulunamayacak-urun-xyz");

        // Act
        var result = ApplySpec(spec);

        // Assert
        result.Should().BeEmpty();
    }

    // ── KATEGORi FiLTRESi ───────────────────────────────────────────────

    [Fact]
    public void CategoryFilter_ShouldReturnOnlyMatchingCategory()
    {
        // Arrange — telefon kategorisi
        var phoneCategory = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var spec = new ProductFilterSpecification(categoryId: phoneCategory);

        // Act
        var result = ApplySpec(spec);

        // Assert — iPhone ve Samsung (pasif olan haric)
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.CategoryId == phoneCategory);
    }

    // ── FiYAT ARALIGI ───────────────────────────────────────────────────

    [Fact]
    public void MinPrice_ShouldFilterCheaperProducts()
    {
        // Arrange — 40000 TL ve uzeri
        var spec = new ProductFilterSpecification(minPrice: 40000m);

        // Act
        var result = ApplySpec(spec);

        // Assert — iPhone (55k), Samsung (42k), MacBook (48k)
        result.Should().HaveCount(3);
        result.Should().OnlyContain(p => p.Price >= 40000m);
    }

    [Fact]
    public void MaxPrice_ShouldFilterExpensiveProducts()
    {
        // Arrange — 10000 TL ve alti
        var spec = new ProductFilterSpecification(maxPrice: 10000m);

        // Act
        var result = ApplySpec(spec);

        // Assert — sadece Logitech mouse (3500)
        result.Should().HaveCount(1);
        result.First().Name.Should().Contain("Logitech");
    }

    [Fact]
    public void PriceRange_ShouldFilterBetweenMinAndMax()
    {
        // Arrange — 40000 ile 50000 arasi
        var spec = new ProductFilterSpecification(minPrice: 40000m, maxPrice: 50000m);

        // Act
        var result = ApplySpec(spec);

        // Assert — Samsung (42k) ve MacBook (48k)
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Price >= 40000m && p.Price <= 50000m);
    }

    // ── STOK FiLTRESi ───────────────────────────────────────────────────

    [Fact]
    public void InStock_True_ShouldExcludeOutOfStockProducts()
    {
        // Arrange — sadece stokta olanlar
        var spec = new ProductFilterSpecification(inStock: true);

        // Act
        var result = ApplySpec(spec);

        // Assert — MacBook (stock=0) gelmemeli
        result.Should().HaveCount(3);
        result.Should().NotContain(p => p.Name == "MacBook Air M3");
        result.Should().OnlyContain(p => p.StockQuantity > 0);
    }

    // ── KOMBiNE FiLTRELER ───────────────────────────────────────────────

    [Fact]
    public void CombinedFilters_SearchAndCategory_ShouldWork()
    {
        // Arrange — telefon kategorisinde "apple" ara
        var phoneCategory = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var spec = new ProductFilterSpecification(
            searchTerm: "apple",
            categoryId: phoneCategory);

        // Act
        var result = ApplySpec(spec);

        // Assert — sadece iPhone (MacBook farkli kategoride)
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("iPhone 15 Pro");
    }

    [Fact]
    public void CombinedFilters_PriceAndInStock_ShouldWork()
    {
        // Arrange — 40k+ ve stokta
        var spec = new ProductFilterSpecification(
            minPrice: 40000m,
            inStock: true);

        // Act
        var result = ApplySpec(spec);

        // Assert — iPhone (55k, stok:25) ve Samsung (42k, stok:30)
        // MacBook (48k ama stok:0) gelmemeli
        result.Should().HaveCount(2);
        result.Should().NotContain(p => p.StockQuantity == 0);
    }

    // ── SIRALAMA ─────────────────────────────────────────────────────────

    [Fact]
    public void SortByPrice_Ascending_ShouldOrderCorrectly()
    {
        // Arrange
        var spec = new ProductFilterSpecification(sortBy: "price", sortDescending: false);

        // Act
        var result = ApplySpec(spec);

        // Assert — ucuzdan pahaliya
        result.First().Name.Should().Contain("Logitech");  // 3500
        result.Last().Name.Should().Contain("iPhone");       // 55000
    }

    [Fact]
    public void SortByPrice_Descending_ShouldOrderCorrectly()
    {
        // Arrange
        var spec = new ProductFilterSpecification(sortBy: "price", sortDescending: true);

        // Act
        var result = ApplySpec(spec);

        // Assert — pahalidan ucuza
        result.First().Name.Should().Contain("iPhone");     // 55000
        result.Last().Name.Should().Contain("Logitech");     // 3500
    }

    [Fact]
    public void SortByName_Ascending_ShouldOrderAlphabetically()
    {
        // Arrange
        var spec = new ProductFilterSpecification(sortBy: "name", sortDescending: false);

        // Act
        var result = ApplySpec(spec);

        // Assert — alfabetik
        result.First().Name.Should().Contain("iPhone");     // I
        result.Last().Name.Should().Contain("Samsung");      // S
    }

    [Fact]
    public void SortByStock_Descending_ShouldOrderCorrectly()
    {
        // Arrange
        var spec = new ProductFilterSpecification(sortBy: "stock", sortDescending: true);

        // Act
        var result = ApplySpec(spec);

        // Assert — en cok stoktan en aza
        result.First().Name.Should().Contain("Logitech");   // 100
        result.Last().StockQuantity.Should().Be(0);           // MacBook
    }

    [Fact]
    public void DefaultSort_ShouldOrderByCreatedAtDescending()
    {
        // Arrange — sortBy belirtilmezse en yeni once
        var spec = new ProductFilterSpecification();

        // Act
        var result = ApplySpec(spec);

        // Assert — en yeni urun ilk sirada
        result.First().Name.Should().Be("MacBook Air M3");  // -3 gun
    }

    // ── SAYFALAMA ────────────────────────────────────────────────────────

    [Fact]
    public void Pagination_FirstPage_ShouldReturnCorrectItems()
    {
        // Arrange — sayfa 1, boyut 2
        var spec = new ProductFilterSpecification(pageNumber: 1, pageSize: 2);

        // Act
        var result = ApplySpec(spec);

        // Assert
        result.Should().HaveCount(2);
        spec.IsPagingEnabled.Should().BeTrue();
        spec.Skip.Should().Be(0);
        spec.Take.Should().Be(2);
    }

    [Fact]
    public void Pagination_SecondPage_ShouldSkipFirstPage()
    {
        // Arrange — sayfa 2, boyut 2
        var spec = new ProductFilterSpecification(pageNumber: 2, pageSize: 2);

        // Act
        var result = ApplySpec(spec);

        // Assert
        result.Should().HaveCount(2);
        spec.Skip.Should().Be(2);  // (2-1) * 2 = 2
        spec.Take.Should().Be(2);
    }

    [Fact]
    public void Pagination_LastPagePartial_ShouldReturnRemainingItems()
    {
        // Arrange — sayfa 2, boyut 3 → 4 aktif urunden 3. sayfada 1 urun kalmali
        var spec = new ProductFilterSpecification(pageNumber: 2, pageSize: 3);

        // Act
        var result = ApplySpec(spec);

        // Assert — 4 aktif urun, ilk sayfa 3 tane, ikinci sayfa 1 tane
        result.Should().HaveCount(1);
    }

    [Fact]
    public void NoPagination_WhenParametersNull_ShouldReturnAll()
    {
        // Arrange — sayfalama parametreleri verilmezse tum sonuclar gelmeli
        var spec = new ProductFilterSpecification();

        // Act & Assert
        spec.IsPagingEnabled.Should().BeFalse();
        var result = ApplySpec(spec);
        result.Should().HaveCount(4); // 4 aktif urun
    }

    // ── INCLUDE'LAR ──────────────────────────────────────────────────────

    [Fact]
    public void Spec_ShouldIncludeCategory()
    {
        // Arrange
        var spec = new ProductFilterSpecification();

        // Assert — Category include edilmis olmali
        spec.Includes.Should().HaveCount(1);
    }

    // ── CURSOR-BASED PAGINATION ──────────────────────────────────────────

    [Fact]
    public void CursorPagination_ShouldFilterByIdGreaterThanCursor()
    {
        // Arrange — cursor olarak ilk urunun ID'sini ver
        var cursorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var spec = new ProductFilterSpecification(
            cursor: cursorId,
            pageSize: 10);

        // Act
        var result = ApplySpec(spec);

        // Assert — cursor ID'den buyuk ID'ye sahip aktif urunler gelmeli
        result.Should().OnlyContain(p => p.Id.CompareTo(cursorId) > 0);
        result.Should().NotContain(p => p.Id == Guid.Parse("00000000-0000-0000-0000-000000000001"));
        result.Should().NotContain(p => p.Id == Guid.Parse("00000000-0000-0000-0000-000000000002"));
    }

    [Fact]
    public void CursorPagination_ShouldNotApplyOffset()
    {
        // Arrange — cursor varken Skip 0 olmali (offset-based degil)
        var cursorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var spec = new ProductFilterSpecification(
            cursor: cursorId,
            pageSize: 5);

        // Assert
        spec.IsPagingEnabled.Should().BeTrue();
        spec.Skip.Should().Be(0);
        spec.Take.Should().Be(5);
    }
}
