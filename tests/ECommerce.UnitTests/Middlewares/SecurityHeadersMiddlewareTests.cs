using ECommerce.API.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ECommerce.UnitTests.Middlewares;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsSecurityHeadersToEveryResponse()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new SecurityHeadersMiddleware(async httpContext =>
        {
            await httpContext.Response.StartAsync();
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers["Content-Security-Policy"].ToString()
            .Should().Contain("frame-ancestors 'none'");
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("no-referrer");
        context.Response.Headers["Permissions-Policy"].ToString().Should().Contain("camera=()");
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedResponseDisablesCaching()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer test-token";
        context.Response.Body = new MemoryStream();
        var middleware = new SecurityHeadersMiddleware(async httpContext =>
        {
            await httpContext.Response.StartAsync();
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
        context.Response.Headers.Pragma.ToString().Should().Be("no-cache");
    }
}
