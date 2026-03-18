using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
               this IServiceCollection services)
        {
            // Şimdilik boş, ilerleyen fazlarda Email, Payment vs. buraya gelecek
            return services;
        }
    }
}
