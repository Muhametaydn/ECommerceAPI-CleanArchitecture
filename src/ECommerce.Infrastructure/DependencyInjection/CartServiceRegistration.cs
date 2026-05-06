using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.DependencyInjection
{
    public static class CartServiceRegistration
    {
        public static IServiceCollection AddCartServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Redis distributed cache yapılandırması
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = "ECommerce:";
            });

            services.AddScoped<ICartService, RedisCartService>();

            return services;
        }
    }
}
