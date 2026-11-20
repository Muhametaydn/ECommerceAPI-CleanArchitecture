namespace ECommerce.Application.Common.Interfaces;

public interface IProductImageStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);
    Task DeleteIfUnreferencedAsync(string? imageUrl, CancellationToken cancellationToken = default);
    Task<int> CleanupOrphansAsync(TimeSpan minimumAge, CancellationToken cancellationToken = default);
}
