using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services;

public sealed class LocalProfileImageStorage : IProfileImageStorage
{
    private const string UrlPrefix = "/uploads/profiles/";
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".png", ".webp" };
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<LocalProfileImageStorage> _logger;

    public LocalProfileImageStorage(IWebHostEnvironment environment, ApplicationDbContext dbContext, ILogger<LocalProfileImageStorage> logger)
    {
        _environment = environment;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Desteklenmeyen profil görseli uzantısı.");

        var directory = GetUploadDirectory();
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using var output = File.Create(Path.Combine(directory, fileName));
        await content.CopyToAsync(output, cancellationToken);
        return $"{UrlPrefix}{fileName}";
    }

    public async Task DeleteIfUnreferencedAsync(string? imageUrl, CancellationToken cancellationToken = default)
    {
        var fileName = GetManagedFileName(imageUrl);
        if (fileName is null)
            return;

        if (await _dbContext.Set<ApplicationUser>().AsNoTracking().AnyAsync(user => user.ProfileImageUrl == imageUrl, cancellationToken))
            return;

        try
        {
            var path = Path.Combine(GetUploadDirectory(), fileName);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Eski profil görseli silinemedi: {FileName}", fileName);
        }
    }

    private string GetUploadDirectory()
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "profiles");
    }

    private static string? GetManagedFileName(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith(UrlPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var fileName = imageUrl[UrlPrefix.Length..];
        return !string.IsNullOrWhiteSpace(fileName)
            && fileName == Path.GetFileName(fileName)
            && AllowedExtensions.Contains(Path.GetExtension(fileName))
                ? fileName
                : null;
    }
}
