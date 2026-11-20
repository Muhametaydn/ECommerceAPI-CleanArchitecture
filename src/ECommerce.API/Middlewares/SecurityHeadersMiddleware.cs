namespace ECommerce.API.Middlewares;

public sealed class SecurityHeadersMiddleware
{
    private const string ApiContentSecurityPolicy =
        "default-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] =
            "camera=(), geolocation=(), microphone=(), payment=(), usb=()";
        headers["Content-Security-Policy"] = ApiContentSecurityPolicy;

        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            headers.CacheControl = "no-store";
            headers.Pragma = "no-cache";
        }

        await _next(context);
    }
}
