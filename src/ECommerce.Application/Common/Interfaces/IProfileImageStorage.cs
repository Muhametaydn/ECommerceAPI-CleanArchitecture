namespace ECommerce.Application.Common.Interfaces;

public interface IProfileImageStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);
    Task DeleteIfUnreferencedAsync(string? imageUrl, CancellationToken cancellationToken = default);
}
