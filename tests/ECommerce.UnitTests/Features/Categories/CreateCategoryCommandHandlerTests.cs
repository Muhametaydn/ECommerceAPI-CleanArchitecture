using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Categories.Commands.CreateCategory;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Categories;

public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly CreateCategoryCommandHandler _handler;

    public CreateCategoryCommandHandlerTests()
    {
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Category).Returns(_categoryRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var cacheMock = new Mock<ICacheService>();
        _handler = new CreateCategoryCommandHandler(_unitOfWorkMock.Object, cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRootCategory_ShouldCreateSuccessfully()
    {
        // Arrange
        _categoryRepoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => c);

        var command = new CreateCategoryCommand
        {
            Name = "Elektronik",
            Description = "Elektronik urunler"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _categoryRepoMock.Verify(r => r.AddAsync(It.Is<Category>(c =>
            c.Name == "Elektronik" &&
            c.Slug == "elektronik" &&
            c.Depth == 1 &&
            c.ParentCategoryId == null
        )), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithParentCategory_ShouldSetDepthCorrectly()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = new Category { Id = parentId, Depth = 1, Name = "Elektronik" };

        _categoryRepoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(parentId)).ReturnsAsync(parent);
        _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => c);

        var command = new CreateCategoryCommand
        {
            Name = "Telefonlar",
            ParentCategoryId = parentId
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _categoryRepoMock.Verify(r => r.AddAsync(It.Is<Category>(c =>
            c.Depth == 2 &&
            c.ParentCategoryId == parentId
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentParent_ShouldThrow()
    {
        // Arrange
        var fakeParentId = Guid.NewGuid();
        _categoryRepoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(fakeParentId)).ReturnsAsync((Category?)null);

        var command = new CreateCategoryCommand
        {
            Name = "Test",
            ParentCategoryId = fakeParentId
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExceedingMaxDepth_ShouldThrow()
    {
        // Arrange — depth=3 parent'a alt eklemeye calis
        var parentId = Guid.NewGuid();
        var deepParent = new Category { Id = parentId, Depth = Category.MaxDepth, Name = "Derin" };

        _categoryRepoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(parentId)).ReturnsAsync(deepParent);

        var command = new CreateCategoryCommand
        {
            Name = "Cok Derin",
            ParentCategoryId = parentId
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ShouldAppendNumber()
    {
        // Arrange — "elektronik" slug'i zaten var
        _categoryRepoMock.Setup(r => r.SlugExistsAsync("elektronik", null)).ReturnsAsync(true);
        _categoryRepoMock.Setup(r => r.SlugExistsAsync("elektronik-2", null)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => c);

        var command = new CreateCategoryCommand { Name = "Elektronik" };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — slug "elektronik-2" olmali
        _categoryRepoMock.Verify(r => r.AddAsync(It.Is<Category>(c =>
            c.Slug == "elektronik-2"
        )), Times.Once);
    }
}
