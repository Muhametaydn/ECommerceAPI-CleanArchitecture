using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using System.Text.Json;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.Infrastructure.Services
{
    public class RedisCartService : ICartService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly string _prefix;

        // Kimliği doğrulanmış kullanıcılar için 30 gün, anonim kullanıcılar için 7 gün
        private static readonly TimeSpan AuthenticatedTtl = TimeSpan.FromDays(30);
        private static readonly TimeSpan AnonymousTtl = TimeSpan.FromDays(7);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public RedisCartService(IDistributedCache cache, IConnectionMultiplexer redis, IOptions<RedisCacheOptions> options)
        {
            _cache = cache;
            _redis = redis;
            _prefix = options.Value.InstanceName ?? "";
        }

        public async Task<Cart?> GetCartAsync(string cartId)
        {
            var json = await _cache.GetStringAsync(cartId);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<Cart>(json, JsonOptions);
        }

        public async Task SaveCartAsync(Cart cart)
        {
            var json = JsonSerializer.Serialize(cart, JsonOptions);
            var ttl = IsAnonymousCart(cart.Id) ? AnonymousTtl : AuthenticatedTtl;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            await _cache.SetStringAsync(cart.Id, json, options);
        }

        public async Task DeleteCartAsync(string cartId)
        {
            await _cache.RemoveAsync(cartId);
        }

        public async Task<bool> DeleteCartIfUnchangedAsync(string cartId, Cart snapshot)
        {
            const string script = "if redis.call('HGET', KEYS[1], 'data') == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
            var result = await _redis.GetDatabase().ScriptEvaluateAsync(script,
                new RedisKey[] { _prefix + cartId }, new RedisValue[] { JsonSerializer.Serialize(snapshot, JsonOptions) });
            return (long)result == 1;
        }

        public async Task<Cart> MergeCartsAsync(string anonymousCartId, string userCartId)
        {
            var anonCart = await GetCartAsync(anonymousCartId);
            var userCart = await GetCartAsync(userCartId)
                          ?? new Cart { Id = userCartId, CreatedAt = DateTime.UtcNow };

            if (anonCart?.Items.Any() == true)
            {
                // Anonim sepetteki ürünleri kullanıcı sepetine aktar
                foreach (var item in anonCart.Items)
                {
                    userCart.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
                }

                // Anonim sepeti sil
                await DeleteCartAsync(anonymousCartId);
            }

            await SaveCartAsync(userCart);
            return userCart;
        }

        private static bool IsAnonymousCart(string cartId) => cartId.StartsWith("cart:anon:");
    }
}
