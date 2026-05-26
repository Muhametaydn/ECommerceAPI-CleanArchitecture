using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// IDistributedCache (Redis) tabanlı generic cache servisi.
/// Prefix bazlı toplu silme için doğrudan IConnectionMultiplexer kullanılır.
/// </summary>
public class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET hatası. Key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET hatası. Key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis REMOVE hatası. Key: {Key}", key);
        }
    }

    /// <summary>
    /// Verilen prefix ile başlayan tüm Redis key'lerini siler.
    /// Örnek: "products:" prefix'i ile tüm ürün cache girdileri temizlenir.
    /// </summary>
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            // InstanceName = "ECommerce:" olduğundan gerçek key'ler "ECommerce:{prefix}*" şeklinde
            var pattern = $"ECommerce:{prefix}*";
            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length == 0) return;

            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(keys);

            _logger.LogDebug("Redis prefix temizlendi: {Prefix} ({Count} key)", prefix, keys.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis RemoveByPrefix hatası. Prefix: {Prefix}", prefix);
        }
    }
}
