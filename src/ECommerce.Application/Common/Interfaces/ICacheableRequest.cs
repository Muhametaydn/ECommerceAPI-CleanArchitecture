namespace ECommerce.Application.Common.Interfaces;

/// <summary>
/// Bu interface'i implement eden MediatR query'leri CachingBehavior tarafından otomatik cache'lenir.
/// </summary>
public interface ICacheableRequest
{
    /// <summary>
    /// Redis'teki cache anahtarı
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Cache süresi. null ise varsayılan TTL kullanılır (5 dakika).
    /// </summary>
    TimeSpan? CacheDuration => null;
}
