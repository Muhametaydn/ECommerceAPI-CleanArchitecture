using System.Net;
using ECommerce.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.IntegrationTests;

// Regression: limitler eskiden bölümlenmemişti; tek istemcinin 5 giriş denemesi
// tüm kullanıcıların girişini 1 dakika boyunca engelliyordu.
public sealed class RateLimitingHttpTests : IAsyncLifetime
{
    private const string TestPeerHeader = "X-Test-Peer";
    private const string TrustedProxy = "10.0.0.1";
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReverseProxy:KnownProxies:0"] = TrustedProxy
        });
        builder.Services.AddTrustedForwardedHeaders(builder.Configuration);
        builder.Services.AddECommerceRateLimiting();

        _app = builder.Build();
        // TestServer bağlantı IP'si vermez; testte TCP eşini başlıktan simüle ediyoruz.
        _app.Use((context, next) =>
        {
            if (context.Request.Headers.TryGetValue(TestPeerHeader, out var peer))
                context.Connection.RemoteIpAddress = IPAddress.Parse(peer!);
            return next();
        });
        _app.UseForwardedHeaders();
        _app.UseRateLimiter();
        _app.MapPost("/login", () => Results.Ok()).RequireRateLimiting(RateLimitingRegistration.AuthPolicy);
        _app.MapGet("/ip", (HttpContext context) => context.Connection.RemoteIpAddress!.ToString());

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Auth_limit_is_per_client_so_one_attacker_cannot_block_everyone()
    {
        for (var i = 0; i < RateLimitingRegistration.AuthPermitPerMinute; i++)
            Assert.Equal(HttpStatusCode.OK, (await Login("203.0.113.10")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login("203.0.113.10")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login("198.51.100.20")).StatusCode);
    }

    [Fact]
    public async Task Clients_behind_trusted_proxy_get_separate_limits()
    {
        for (var i = 0; i < RateLimitingRegistration.AuthPermitPerMinute; i++)
            await Login(TrustedProxy, forwardedFor: "203.0.113.30");

        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await Login(TrustedProxy, forwardedFor: "203.0.113.30")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await Login(TrustedProxy, forwardedFor: "203.0.113.31")).StatusCode);
    }

    [Fact]
    public async Task Forwarded_for_from_untrusted_peer_is_ignored()
    {
        var spoofed = await Send(HttpMethod.Get, "/ip", "198.51.100.99", forwardedFor: "1.2.3.4");
        var proxied = await Send(HttpMethod.Get, "/ip", TrustedProxy, forwardedFor: "1.2.3.4");

        Assert.Equal("198.51.100.99", await spoofed.Content.ReadAsStringAsync());
        Assert.Equal("1.2.3.4", await proxied.Content.ReadAsStringAsync());
    }

    private Task<HttpResponseMessage> Login(string peer, string? forwardedFor = null)
        => Send(HttpMethod.Post, "/login", peer, forwardedFor);

    private Task<HttpResponseMessage> Send(HttpMethod method, string path, string peer, string? forwardedFor)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(TestPeerHeader, peer);
        if (forwardedFor is not null)
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        return _client.SendAsync(request);
    }
}
