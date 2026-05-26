using System.Text;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Contracts.Identity;
using ECommerce.Infrastructure.Consumers;
using ECommerce.Infrastructure.EventBus;
using ECommerce.Infrastructure.Events;
using ECommerce.Infrastructure.Services;
using ECommerce.Infrastructure.Settings;
using System.Net.Mail;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Infrastructure
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Settings ──────────────────────────────────────────────────────
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.SectionName));
            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
            services.Configure<ElasticsearchSettings>(configuration.GetSection(ElasticsearchSettings.SectionName));

            // ── JWT Authentication ────────────────────────────────────────────
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
            var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // ── Core Servisler ─────────────────────────────────────────────────
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddScoped<IUserLookupService, UserLookupService>();

            // ── FluentEmail ───────────────────────────────────────────────────
            var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>()!;
            services
                .AddFluentEmail(emailSettings.From, emailSettings.FromName)
                .AddSmtpSender(new SmtpClient(emailSettings.Host, emailSettings.Port)
                {
                    EnableSsl = emailSettings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = string.IsNullOrEmpty(emailSettings.Username)
                        ? null
                        : new System.Net.NetworkCredential(emailSettings.Username, emailSettings.Password)
                });

            services.AddScoped<IEmailService, EmailService>();

            // ── MassTransit + RabbitMQ ─────────────────────────────────────────
            var rabbitSettings = configuration.GetSection(RabbitMQSettings.SectionName).Get<RabbitMQSettings>()!;

            services.AddMassTransit(x =>
            {
                // Consumer'ları kaydet
                x.AddConsumer<OrderCreatedConsumer>();
                x.AddConsumer<OrderStatusChangedConsumer>();
                x.AddConsumer<PaymentProcessedConsumer>();
                x.AddConsumer<LowStockAlertConsumer>();

                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(rabbitSettings.Host, rabbitSettings.VirtualHost, h =>
                    {
                        h.Username(rabbitSettings.Username);
                        h.Password(rabbitSettings.Password);
                    });

                    // Her consumer için otomatik queue yapılandır
                    cfg.ConfigureEndpoints(ctx);
                });
            });

            services.AddScoped<IEventBus, MassTransitEventBus>();

            // ── Elasticsearch ─────────────────────────────────────────────────
            services.AddScoped<ISearchService, ElasticsearchService>();

            return services;
        }
    }
}
