using ECommerce.Application.Common.Interfaces;
using Hangfire;

namespace ECommerce.API.Jobs;

public sealed class ProductImageCleanupJob
{
    private static readonly TimeSpan OrphanGracePeriod = TimeSpan.FromHours(24);
    private readonly IProductImageStorage _storage;
    private readonly ILogger<ProductImageCleanupJob> _logger;

    public ProductImageCleanupJob(
        IProductImageStorage storage,
        ILogger<ProductImageCleanupJob> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var deletedCount = await _storage.CleanupOrphansAsync(OrphanGracePeriod, cancellationToken);
        if (deletedCount > 0)
            _logger.LogInformation("{Count} kullanılmayan ürün görseli temizlendi.", deletedCount);
    }
}
