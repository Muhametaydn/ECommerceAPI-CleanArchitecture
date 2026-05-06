using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Entities;

/// <summary>
/// Category entity business logic testleri.
/// Domain katmanindaki is kurallari DB olmadan test edilir.
/// </summary>
public class CategoryTests
{
    // ── SLUG OLUSTURMA ───────────────────────────────────────────────────

    [Theory]
    [InlineData("Elektronik", "elektronik")]
    [InlineData("Akıllı Telefonlar", "akilli-telefonlar")]
    [InlineData("Ev & Yaşam", "ev-yasam")] // & kaldirilir, coklu tireler teke duser
    [InlineData("Giyim Ürünleri", "giyim-urunleri")]
    [InlineData("BÜYÜK HARFLER", "buyuk-harfler")]
    public void GenerateSlug_ShouldConvertCorrectly(string name, string expectedSlug)
    {
        // Act
        var slug = Category.GenerateSlug(name);

        // Assert
        slug.Should().Be(expectedSlug);
    }

    [Fact]
    public void GenerateSlug_WithEmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => Category.GenerateSlug("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateSlug_WithWhitespace_ShouldThrow()
    {
        var act = () => Category.GenerateSlug("   ");
        act.Should().Throw<ArgumentException>();
    }

    // ── DERINLIK KONTROLU ────────────────────────────────────────────────

    [Fact]
    public void CanAddSubCategory_Depth1_ShouldReturnTrue()
    {
        // Arrange — root kategori (depth=1), max=3
        var root = new Category { Depth = 1 };

        // Act & Assert
        root.CanAddSubCategory().Should().BeTrue();
    }

    [Fact]
    public void CanAddSubCategory_Depth2_ShouldReturnTrue()
    {
        var mid = new Category { Depth = 2 };
        mid.CanAddSubCategory().Should().BeTrue();
    }

    [Fact]
    public void CanAddSubCategory_Depth3_ShouldReturnFalse()
    {
        // Arrange — en derin seviye, altina eklenemez
        var leaf = new Category { Depth = 3 };

        // Act & Assert
        leaf.CanAddSubCategory().Should().BeFalse();
    }

    // ── SET PARENT ───────────────────────────────────────────────────────

    [Fact]
    public void SetParent_WithValidParent_ShouldSetDepthCorrectly()
    {
        // Arrange
        var parent = new Category { Id = Guid.NewGuid(), Depth = 1, Name = "Elektronik" };
        var child = new Category { Id = Guid.NewGuid(), Name = "Telefonlar" };

        // Act
        child.SetParent(parent);

        // Assert
        child.ParentCategoryId.Should().Be(parent.Id);
        child.Depth.Should().Be(2);
    }

    [Fact]
    public void SetParent_ChainedHierarchy_ShouldCalculateDepthCorrectly()
    {
        // Arrange — Elektronik(1) > Telefonlar(2) > Akilli Telefonlar(3)
        var root = new Category { Id = Guid.NewGuid(), Depth = 1, Name = "Elektronik" };
        var mid = new Category { Id = Guid.NewGuid(), Name = "Telefonlar" };
        mid.SetParent(root);

        var leaf = new Category { Id = Guid.NewGuid(), Name = "Akilli Telefonlar" };
        leaf.SetParent(mid);

        // Assert
        mid.Depth.Should().Be(2);
        leaf.Depth.Should().Be(3);
    }

    [Fact]
    public void SetParent_ExceedingMaxDepth_ShouldThrow()
    {
        // Arrange — depth=3 olan kategoriye alt eklemeye calis
        var leaf = new Category { Id = Guid.NewGuid(), Depth = Category.MaxDepth, Name = "En derin" };
        var tooDeep = new Category { Id = Guid.NewGuid(), Name = "Cok derin" };

        // Act & Assert
        var act = () => tooDeep.SetParent(leaf);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Maksimum kategori derinligi*");
    }

    [Fact]
    public void SetParent_SelfReference_ShouldThrow()
    {
        // Arrange — kategori kendisinin alt kategorisi olamaz
        var category = new Category { Id = Guid.NewGuid(), Depth = 1, Name = "Test" };

        // Act & Assert
        var act = () => category.SetParent(category);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*kendisinin alt kategorisi*");
    }

    [Fact]
    public void SetParent_Null_ShouldMakeRoot()
    {
        // Arrange — mevcut parent'i kaldir, root yap
        var child = new Category
        {
            Id = Guid.NewGuid(),
            ParentCategoryId = Guid.NewGuid(),
            Depth = 2
        };

        // Act
        child.SetParent(null);

        // Assert
        child.ParentCategoryId.Should().BeNull();
        child.Depth.Should().Be(1);
    }

    // ── ACTIVATE / DEACTIVATE ────────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var category = new Category { IsActive = true };

        category.Deactivate();

        category.IsActive.Should().BeFalse();
        category.UpdateAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var category = new Category { IsActive = false };

        category.Activate();

        category.IsActive.Should().BeTrue();
        category.UpdateAt.Should().NotBeNull();
    }
}
