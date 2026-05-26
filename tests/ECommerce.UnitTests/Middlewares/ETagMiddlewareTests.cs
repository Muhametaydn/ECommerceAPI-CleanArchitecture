using ECommerce.API.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.UnitTests.Middlewares;

/// <summary>
/// ETagMiddleware testleri.
///
/// Senaryolar:
/// 1. GET + 200 + JSON → ETag header eklenir.
/// 2. GET + If-None-Match eşleşirse → 304 Not Modified döner.
/// 3. POST/PUT/DELETE/PATCH → middleware bypass edilir.
/// 4. GET + 200 + non-JSON content type → ETag eklenmez.
/// 5. GET + 4xx/5xx → ETag eklenmez.
/// 6. ETag değeri RFC 7232 formatına uygun (çift tırnaklı).
/// 7. If-None-Match eşleşmiyorsa → 200 dönmeye devam eder.
/// </summary>
public class ETagMiddlewareTests
{
    private readonly Mock<ILogger<ETagMiddleware>> _loggerMock = new();

    // ── Yardımcı ─────────────────────────────────────────────────────────

    /// <summary>Verilen body, status ve content-type ile bir HttpContext oluşturur.</summary>
    private static DefaultHttpContext BuildContext(
        string method = "GET",
        int statusCode = 200,
        string contentType = "application/json; charset=utf-8",
        string body = "{\"id\":1}",
        string? ifNoneMatch = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = contentType;
        ctx.Response.Body = new MemoryStream();

        if (ifNoneMatch is not null)
            ctx.Request.Headers.IfNoneMatch = ifNoneMatch;

        return ctx;
    }

    private static string ComputeExpectedETag(string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var hash = MD5.HashData(bytes);
        return $"\"{Convert.ToHexString(hash).ToLowerInvariant()}\"";
    }

    private ETagMiddleware BuildMiddleware(string body, int statusCode = 200,
        string contentType = "application/json")
    {
        return new ETagMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = statusCode;
                ctx.Response.ContentType = contentType;
                var bodyBytes = Encoding.UTF8.GetBytes(body);
                await ctx.Response.Body.WriteAsync(bodyBytes);
            },
            logger: _loggerMock.Object);
    }

    // ── ETag EKLENMESİ ────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenGetRequest_WithJsonResponse_ShouldAddETagHeader()
    {
        // Arrange
        const string responseBody = "{\"name\":\"laptop\",\"price\":4999}";
        var middleware = BuildMiddleware(responseBody);

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.Headers.ETag.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ETagValue_ShouldMatchMd5HashOfBody()
    {
        // Arrange
        const string responseBody = "{\"id\":42,\"name\":\"Telefon\"}";
        var middleware = BuildMiddleware(responseBody);

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        var expected = ComputeExpectedETag(responseBody);
        ctx.Response.Headers.ETag.ToString().Should().Be(expected);
    }

    [Fact]
    public async Task InvokeAsync_ETagValue_ShouldBeRfc7232Compliant_QuotedString()
    {
        // Arrange
        var middleware = BuildMiddleware("{\"ok\":true}");

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert — RFC 7232: ETag değerleri çift tırnak içinde olmalı
        var etag = ctx.Response.Headers.ETag.ToString();
        etag.Should().StartWith("\"").And.EndWith("\"");
    }

    // ── 304 NOT MODIFIED ──────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenIfNoneMatchMatchesETag_ShouldReturn304()
    {
        // Arrange
        const string responseBody = "{\"id\":1}";
        var expectedETag = ComputeExpectedETag(responseBody);
        var middleware = BuildMiddleware(responseBody);

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Headers.IfNoneMatch = expectedETag;
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(304);
    }

    [Fact]
    public async Task InvokeAsync_WhenIfNoneMatchDoesNotMatch_ShouldReturn200()
    {
        // Arrange
        var middleware = BuildMiddleware("{\"id\":2}");

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Headers.IfNoneMatch = "\"different-etag-value\"";
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_When304Returned_ResponseBodyShouldBeEmpty()
    {
        // Arrange
        const string responseBody = "{\"id\":1}";
        var expectedETag = ComputeExpectedETag(responseBody);
        var middleware = BuildMiddleware(responseBody);

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Headers.IfNoneMatch = expectedETag;
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert — 304'te body olmaz
        ctx.Response.ContentLength.Should().Be(0);
    }

    // ── HTTP METOD BYPASS ─────────────────────────────────────────────────

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task InvokeAsync_WhenNonGetMethod_ShouldNotAddETagHeader(string method)
    {
        // Arrange
        var nextCalled = false;
        var middleware = new ETagMiddleware(
            next: ctx => { nextCalled = true; return Task.CompletedTask; },
            logger: _loggerMock.Object);

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        nextCalled.Should().BeTrue();
        ctx.Response.Headers.ETag.ToString().Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WhenHeadRequest_ShouldAddETagHeader()
    {
        // Arrange
        var middleware = BuildMiddleware("{\"count\":5}");

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "HEAD";
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert — HEAD istekleri de ETag almalı (GET gibi davranır)
        ctx.Response.Headers.ETag.ToString().Should().NotBeNullOrEmpty();
    }

    // ── CONTENT TYPE BYPASS ───────────────────────────────────────────────

    [Theory]
    [InlineData("text/html")]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    public async Task InvokeAsync_WhenNonJsonContentType_ShouldNotAddETagHeader(string contentType)
    {
        // Arrange
        var middleware = new ETagMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = contentType;
                await ctx.Response.Body.WriteAsync("<html>ok</html>"u8.ToArray());
            },
            logger: _loggerMock.Object);

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.Headers.ETag.ToString().Should().BeNullOrEmpty();
    }

    // ── HTTP STATUS BYPASS ────────────────────────────────────────────────

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task InvokeAsync_WhenNon2xxStatus_ShouldNotAddETagHeader(int statusCode)
    {
        // Arrange
        var middleware = new ETagMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = statusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Body.WriteAsync("{\"error\":\"not found\"}"u8.ToArray());
            },
            logger: _loggerMock.Object);

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.Headers.ETag.ToString().Should().BeNullOrEmpty();
    }

    // ── BOŞ BODY ─────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenResponseBodyIsEmpty_ShouldNotAddETagHeader()
    {
        // Arrange
        var middleware = new ETagMiddleware(
            next: ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                return Task.CompletedTask; // body yok
            },
            logger: _loggerMock.Object);

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.Headers.ETag.ToString().Should().BeNullOrEmpty();
    }

    // ── DETERMINISTIK ─────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_SameBodyTwice_ShouldProduceSameETag()
    {
        // Arrange
        const string body = "{\"stable\":true}";

        async Task<string> GetETag()
        {
            var middleware = BuildMiddleware(body);
            var ctx = new DefaultHttpContext();
            ctx.Request.Method = "GET";
            ctx.Response.Body = new MemoryStream();
            await middleware.InvokeAsync(ctx);
            return ctx.Response.Headers.ETag.ToString();
        }

        // Act
        var etag1 = await GetETag();
        var etag2 = await GetETag();

        // Assert — ETag deterministik olmalı
        etag1.Should().Be(etag2);
    }

    [Fact]
    public async Task InvokeAsync_DifferentBodies_ShouldProduceDifferentETags()
    {
        // Arrange
        async Task<string> GetETag(string body)
        {
            var middleware = BuildMiddleware(body);
            var ctx = new DefaultHttpContext();
            ctx.Request.Method = "GET";
            ctx.Response.Body = new MemoryStream();
            await middleware.InvokeAsync(ctx);
            return ctx.Response.Headers.ETag.ToString();
        }

        // Act
        var etag1 = await GetETag("{\"price\":100}");
        var etag2 = await GetETag("{\"price\":200}");

        // Assert
        etag1.Should().NotBe(etag2);
    }
}
