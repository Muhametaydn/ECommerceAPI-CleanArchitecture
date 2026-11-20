using ECommerce.API.Controllers;
using ECommerce.Application.Common.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ECommerce.UnitTests.Features.Products;

public sealed class ProductImagesControllerTests
{
    [Fact]
    public async Task Upload_WithUnsupportedContentType_ReturnsBadRequest()
    {
        var controller = CreateController(Mock.Of<IProductImageStorage>());
        var file = CreateFile([1, 2, 3], "test.gif", "image/gif");

        var result = await controller.Upload(file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_WithValidPng_PersistsFileAndReturnsRelativeUrl()
    {
        const string url = "/uploads/products/saved.png";
        var storage = new Mock<IProductImageStorage>();
        storage.Setup(value => value.SaveAsync(It.IsAny<Stream>(), ".png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(url);
        var controller = CreateController(storage.Object);
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
        var file = CreateFile(png, "product.png", "image/png");

        var result = await controller.Upload(file, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        var response = created.Value.Should().BeOfType<ProductImageUploadResponse>().Subject;
        response.Url.Should().Be(url);
        storage.Verify(value => value.SaveAsync(It.IsAny<Stream>(), ".png", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ProductImagesController CreateController(IProductImageStorage storage)
    {
        return new ProductImagesController(storage)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static FormFile CreateFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
