using ECommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Common.Behaviors;

/// <summary>
/// ICacheableRequest implement eden tüm query'leri otomatik olarak Redis'te cache'leyen MediatR pipeline behavior.
/// </summary>
public class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        ICacheService cacheService,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Sadece ICacheableRequest olan query'leri yakala
        if (request is not ICacheableRequest cacheableRequest)
            return await next();

        var cacheKey = cacheableRequest.CacheKey;
        var duration = cacheableRequest.CacheDuration ?? DefaultCacheDuration;

        // Cache'den oku
        var cached = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
            return cached;
        }

        // Cache miss → handler'ı çalıştır
        _logger.LogDebug("Cache MISS: {CacheKey} — DB'den alınıyor", cacheKey);
        var response = await next();

        // Cache'e yaz
        if (response is not null)
            await _cacheService.SetAsync(cacheKey, response, duration, cancellationToken);

        return response;
    }
}
