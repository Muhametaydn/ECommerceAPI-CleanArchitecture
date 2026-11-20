using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Services;

/// <summary>
/// Rate limit politikaları ve güvenilir proxy (X-Forwarded-For) ayarları.
/// Tüm limitler istemci başına bölümlenir; aksi halde tek bir istemci herkesi kilitleyebilir.
/// </summary>
public static class RateLimitingRegistration
{
    public const string AuthPolicy = "auth";
    public const string ApiPolicy = "api";

    public const int AuthPermitPerMinute = 5;
    public const int ApiPermitPerMinute = 30;
    public const int GlobalPermitPerMinute = 60;

    public static IServiceCollection AddECommerceRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Kimlik doğrulama uçları: her zaman IP başına (giriş öncesi kullanıcı kimliği yok).
            options.AddPolicy(AuthPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                $"ip:{ClientIp(context)}",
                _ => Window(AuthPermitPerMinute, queueLimit: 0)));

            // Genel API: oturum açmış kullanıcı başına, anonimse IP başına.
            options.AddPolicy(ApiPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(context),
                _ => Window(ApiPermitPerMinute, queueLimit: 2)));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => Window(GlobalPermitPerMinute, queueLimit: 0)));
        });

        return services;
    }

    /// <summary>
    /// Yalnızca yapılandırılmış proxy'lerden gelen X-Forwarded-For başlığına güvenir.
    /// Varsayılan: sadece loopback. Docker/üretimde ReverseProxy:KnownProxies veya
    /// ReverseProxy:KnownNetworks (CIDR) ile frontend sunucusunun adresi verilmelidir.
    /// </summary>
    public static IServiceCollection AddTrustedForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(proxy, out var address))
                    options.KnownProxies.Add(address);
            }

            foreach (var network in configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
            {
                var parts = network.Split('/', 2);
                if (parts.Length == 2
                    && IPAddress.TryParse(parts[0], out var prefix)
                    && int.TryParse(parts[1], out var length))
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, length));
            }
        });

        return services;
    }

    private static FixedWindowRateLimiterOptions Window(int permits, int queueLimit) => new()
    {
        PermitLimit = permits,
        Window = TimeSpan.FromMinutes(1),
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = queueLimit
    };

    private static string ClientKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(userId) ? $"ip:{ClientIp(context)}" : $"user:{userId}";
    }

    private static string ClientIp(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
