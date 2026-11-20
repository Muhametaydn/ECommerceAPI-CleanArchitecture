using Asp.Versioning;
using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/product-images")]
[Authorize(Policy = "SellerOrAdmin")]
[EnableRateLimiting("api")]
public sealed class ProductImagesController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly Dictionary<string, (string Extension, byte[][] Signatures)> AllowedImages = new()
    {
        ["image/jpeg"] = (".jpg", [[0xFF, 0xD8, 0xFF]]),
        ["image/png"] = (".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
        ["image/webp"] = (".webp", ["RIFF"u8.ToArray()])
    };

    private readonly IProductImageStorage _storage;

    public ProductImagesController(IProductImageStorage storage)
    {
        _storage = storage;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(ProductImageUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Boş dosya yüklenemez." });

        if (file.Length > MaxFileSize)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ProblemDetails
            {
                Title = "Görsel en fazla 5 MB olabilir."
            });

        if (!AllowedImages.TryGetValue(file.ContentType.ToLowerInvariant(), out var imageType))
            return BadRequest(new ProblemDetails { Title = "Yalnızca JPEG, PNG veya WebP görselleri yüklenebilir." });

        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await input.ReadAsync(header, cancellationToken);
        input.Position = 0;

        var hasValidSignature = imageType.Signatures.Any(signature =>
            bytesRead >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));

        if (file.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
        {
            hasValidSignature = bytesRead >= 12 &&
                header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        }

        if (!hasValidSignature)
            return BadRequest(new ProblemDetails { Title = "Dosya içeriği geçerli bir görsel değil." });

        var relativeUrl = await _storage.SaveAsync(input, imageType.Extension, cancellationToken);
        return Created(relativeUrl, new ProductImageUploadResponse(relativeUrl));
    }
}

public sealed record ProductImageUploadResponse(string Url);
