using ECommerce.Domain.Interfaces;
using ECommerce.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Persistence
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPersistenceServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            //Generic repository
            services.AddScoped(typeof(IGenericRepository<>) , typeof(GenericRepository<>));

            //Specific repositories
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();

            //Unit Of Work
            services.AddScoped<IUnitOfWork ,  UnitOfWork>();

            return services;
        }






    }
}
