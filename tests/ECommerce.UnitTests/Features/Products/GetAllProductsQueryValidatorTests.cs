using ECommerce.Application.Features.Products.Queries.GetAllProducts;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ECommerce.UnitTests.Features.Products;

/// <summary>
/// GetAllProductsQueryValidator testleri.
///
/// FluentValidation'in TestHelper'i ile calisiyoruz:
/// validator.TestValidate(model) → .ShouldNotHaveValidationErrorFor(...)
///
/// Bu testler, API'ye gelen yanlis parametrelerin erken yakalanmasini garanti eder.
/// </summary>
public class GetAllProductsQueryValidatorTests
{
    private readonly GetAllProductsQueryValidator _validator;

    public GetAllProductsQueryValidatorTests()
    {
        _validator = new GetAllProductsQueryValidator();
    }

    // ── GECERLI ISTEKLER ─────────────────────────────────────────────────

    [Fact]
    public void ValidQuery_ShouldPassValidation()
    {
        // Arrange
        var query = new GetAllProductsQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "laptop",
            SortBy = "price",
            MinPrice = 100,
            MaxPrice = 5000
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DefaultQuery_ShouldPassValidation()
    {
        // Arrange — hicbir parametre verilmezse
        var query = new GetAllProductsQuery();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── SAYFA NUMARASI ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void PageNumber_LessThan1_ShouldFail(int pageNumber)
    {
        // Arrange
        var query = new GetAllProductsQuery { PageNumber = pageNumber };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    // ── SAYFA BOYUTU ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    [InlineData(100)]
    public void PageSize_OutOfRange_ShouldFail(int pageSize)
    {
        // Arrange
        var query = new GetAllProductsQuery { PageSize = pageSize };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(50)]
    public void PageSize_WithinRange_ShouldPass(int pageSize)
    {
        // Arrange
        var query = new GetAllProductsQuery { PageSize = pageSize };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    // ── FiYAT VALIDASYONU ────────────────────────────────────────────────

    [Fact]
    public void MinPrice_Negative_ShouldFail()
    {
        // Arrange
        var query = new GetAllProductsQuery { MinPrice = -10m };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPrice);
    }

    [Fact]
    public void MaxPrice_Negative_ShouldFail()
    {
        // Arrange
        var query = new GetAllProductsQuery { MaxPrice = -5m };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPrice);
    }

    [Fact]
    public void MinPrice_GreaterThanMaxPrice_ShouldFail()
    {
        // Arrange — min > max mantikli degil
        var query = new GetAllProductsQuery
        {
            MinPrice = 5000m,
            MaxPrice = 1000m
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MinPrice_EqualToMaxPrice_ShouldPass()
    {
        // Arrange — tam fiyat filtresi (min == max)
        var query = new GetAllProductsQuery
        {
            MinPrice = 1000m,
            MaxPrice = 1000m
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── SIRALAMA VALIDASYONU ─────────────────────────────────────────────

    [Theory]
    [InlineData("price")]
    [InlineData("name")]
    [InlineData("stock")]
    [InlineData("date")]
    public void SortBy_ValidValues_ShouldPass(string sortBy)
    {
        // Arrange
        var query = new GetAllProductsQuery { SortBy = sortBy };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("random")]
    [InlineData("popularity")]
    [InlineData("xyz")]
    public void SortBy_InvalidValues_ShouldFail(string sortBy)
    {
        // Arrange
        var query = new GetAllProductsQuery { SortBy = sortBy };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }

    [Fact]
    public void SortBy_Null_ShouldPass()
    {
        // Arrange — sortBy verilmezse varsayilan kullanilir
        var query = new GetAllProductsQuery { SortBy = null };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
    }

    // ── ARAMA TERiMi VALIDASYONU ─────────────────────────────────────────

    [Fact]
    public void SearchTerm_TooLong_ShouldFail()
    {
        // Arrange — 100 karakterden uzun
        var query = new GetAllProductsQuery { SearchTerm = new string('a', 101) };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SearchTerm);
    }

    [Fact]
    public void SearchTerm_ExactlyMaxLength_ShouldPass()
    {
        // Arrange — tam 100 karakter
        var query = new GetAllProductsQuery { SearchTerm = new string('a', 100) };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
    }
}
