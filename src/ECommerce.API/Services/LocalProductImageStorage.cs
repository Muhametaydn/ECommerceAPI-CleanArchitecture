using ECommerce.Application.Common.Interfaces;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services;

public sealed class LocalProductImageStorage : IProductImageStorage
{
    private const string UrlPrefix = "/uploads/products/";
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".png", ".webp"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<LocalProductImageStorage> _logger;

    public LocalProductImageStorage(
        IWebHostEnvironment environment,
        ApplicationDbContext dbContext,
        ILogger<LocalProductImageStorage> logger)
    {
        _environment = environment;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Desteklenmeyen ürün görseli uzantısı.");

        var directory = GetUploadDirectory();
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var destination = Path.Combine(directory, fileName);
        await using var output = File.Create(destination);
        await content.CopyToAsync(output, cancellationToken);

        return $"{UrlPrefix}{fileName}";
    }

    public async Task DeleteIfUnreferencedAsync(
        string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        var fileName = GetManagedFileName(imageUrl);
        if (fileName is null)
            return;

        var isReferenced = await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(product => product.ImageUrl == imageUrl, cancellationToken);
        if (isReferenced)
            return;

        DeleteFile(fileName);
    }

    public async Task<int> CleanupOrphansAsync(
        TimeSpan minimumAge,
        CancellationToken cancellationToken = default)
    {
        var directory = GetUploadDirectory();
        if (!Directory.Exists(directory))
            return 0;

        var referencedFiles = (await _dbContext.Products
                .AsNoTracking()
                .Where(product => product.ImageUrl != null && product.ImageUrl.StartsWith(UrlPrefix))
                .Select(product => product.ImageUrl!)
                .ToListAsync(cancellationToken))
            .Select(GetManagedFileName)
            .Where(fileName => fileName is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cutoff = DateTime.UtcNow.Subtract(minimumAge);
        var deletedCount = 0;

        foreach (var path in Directory.EnumerateFiles(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(path);
            if (!AllowedExtensions.Contains(Path.GetExtension(fileName))
                || referencedFiles.Contains(fileName)
                || File.GetLastWriteTimeUtc(path) > cutoff)
                continue;

            try
            {
                File.Delete(path);
                deletedCount++;
            }
            catch (IOException exception)
            {
                _logger.LogWarning(exception, "Yetim ürün görseli silinemedi: {FileName}", fileName);
            }
            catch (UnauthorizedAccessException exception)
            {
                _logger.LogWarning(exception, "Yetim ürün görseline erişilemedi: {FileName}", fileName);
            }
        }

        return deletedCount;
    }

    private string GetUploadDirectory()
    {
        var webRoot = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "products");
    }

    private static string? GetManagedFileName(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)
            || !imageUrl.StartsWith(UrlPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var fileName = imageUrl[UrlPrefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName != Path.GetFileName(fileName)
            || !AllowedExtensions.Contains(Path.GetExtension(fileName)))
            return null;

        return fileName;
    }

    private void DeleteFile(string fileName)
    {
        var path = Path.Combine(GetUploadDirectory(), fileName);
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Eski ürün görseli silinemedi: {FileName}", fileName);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Eski ürün görseline erişilemedi: {FileName}", fileName);
        }
    }
}
