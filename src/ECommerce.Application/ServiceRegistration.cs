using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Application.Common.Behaviors;

namespace ECommerce.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(StockCacheInvalidationBehavior<,>));
        });
        services.AddAutoMapper(_ => { }, Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Fraud varsayılan olarak kapalı. API, "FraudDetection" ayarını okuyup bu kaydın yerine geçer.
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions
            .TryAddSingleton(services, new Common.Fraud.FraudDetectionSettings());
        services.AddScoped<Common.Fraud.IFraudGate, Common.Fraud.FraudGate>();
        return services;
    }
}
