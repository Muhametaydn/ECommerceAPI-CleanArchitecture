using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.DependencyInjection
{
    public static class CartServiceRegistration
    {
        public static IServiceCollection AddCartServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Redis")!;

            // IConnectionMultiplexer — prefix bazlı silme için (RedisCacheService kullanır)
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConnection));

            // Redis distributed cache yapılandırması
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "ECommerce:";
            });

            // Cart servisi
            services.AddScoped<ICartService, RedisCartService>();

            // Genel cache servisi (products, categories vb.)
            services.AddScoped<ICacheService, RedisCacheService>();

            return services;
        }
    }
}
