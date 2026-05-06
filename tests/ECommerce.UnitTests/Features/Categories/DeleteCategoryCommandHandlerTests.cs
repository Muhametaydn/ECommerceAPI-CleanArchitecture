using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Categories.Commands.DeleteCategory;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Categories;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly DeleteCategoryCommandHandler _handler;

    public DeleteCategoryCommandHandlerTests()
    {
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Category).Returns(_categoryRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _handler = new DeleteCategoryCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyCategory_ShouldDeleteSuccessfully()
    {
        // Arrange — alt kategorisi ve urunu yok
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Bos Kategori" };

        _categoryRepoMock.Setup(r => r.GetCategoryWithSubCategoriesAsync(categoryId))
            .ReturnsAsync(category);
        _categoryRepoMock.Setup(r => r.HasProductsAsync(categoryId))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(new DeleteCategoryCommand(categoryId), CancellationToken.None);

        // Assert
        _categoryRepoMock.Verify(r => r.Delete(category), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var fakeId = Guid.NewGuid();
        _categoryRepoMock.Setup(r => r.GetCategoryWithSubCategoriesAsync(fakeId))
            .ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(new DeleteCategoryCommand(fakeId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CategoryWithSubCategories_ShouldThrow()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Elektronik",
            SubCategories = new List<Category>
            {
                new Category { Name = "Telefonlar" }
            }
        };

        _categoryRepoMock.Setup(r => r.GetCategoryWithSubCategoriesAsync(categoryId))
            .ReturnsAsync(category);

        // Act & Assert
        var act = () => _handler.Handle(new DeleteCategoryCommand(categoryId), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*alt kategori*");
    }

    [Fact]
    public async Task Handle_CategoryWithProducts_ShouldThrow()
    {
        // Arrange — alt kategori yok ama urun var
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Giyim" };

        _categoryRepoMock.Setup(r => r.GetCategoryWithSubCategoriesAsync(categoryId))
            .ReturnsAsync(category);
        _categoryRepoMock.Setup(r => r.HasProductsAsync(categoryId))
            .ReturnsAsync(true);

        // Act & Assert
        var act = () => _handler.Handle(new DeleteCategoryCommand(categoryId), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*urunler var*");
    }
}
