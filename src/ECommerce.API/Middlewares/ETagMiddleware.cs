using System.Security.Cryptography;
using System.Text;

namespace ECommerce.API.Middlewares;

/// <summary>
/// GET ve HEAD istekleri için ETag / If-None-Match HTTP cache validation middleware.
///
/// Nasıl çalışır:
/// 1. Response body'yi buffer'a yazar.
/// 2. Body içeriğinin MD5 hash'ini hesaplar → ETag header olarak ekler.
/// 3. İstemci bir sonraki istekte "If-None-Match: {etag}" header'ı gönderirse
///    ve değer eşleşiyorsa, body gönderilmeden 304 Not Modified döner.
///
/// Uygulanan endpoint'ler: GET + 2xx yanıtlar, JSON içerik tipi.
/// Hariç tutulanlar: Auth endpoint'leri, write işlemleri (POST/PUT/DELETE/PATCH).
/// </summary>
public class ETagMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ETagMiddleware> _logger;

    public ETagMiddleware(RequestDelegate next, ILogger<ETagMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Sadece GET ve HEAD isteklerine uygula
        if (!HttpMethods.IsGet(context.Request.Method) &&
            !HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;

        await using var bufferedBody = new MemoryStream();
        context.Response.Body = bufferedBody;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        // Sadece başarılı JSON yanıtlara ETag ekle
        var isSuccess = context.Response.StatusCode is >= 200 and < 300;
        var isJson = context.Response.ContentType?.Contains("application/json") == true;

        if (!isSuccess || !isJson || bufferedBody.Length == 0)
        {
            bufferedBody.Seek(0, SeekOrigin.Begin);
            await bufferedBody.CopyToAsync(originalBody);
            return;
        }

        // ETag hesapla (MD5 hex)
        bufferedBody.Seek(0, SeekOrigin.Begin);
        var bodyBytes = bufferedBody.ToArray();
        var etag = ComputeETag(bodyBytes);

        // If-None-Match kontrolü
        var ifNoneMatch = context.Request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == etag)
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            context.Response.ContentLength = 0;
            context.Response.Headers.ETag = etag;
            _logger.LogDebug("ETag match — 304 Not Modified: {Path}", context.Request.Path);
            return;
        }

        // ETag header'ını ekle ve body'yi yaz
        context.Response.Headers.ETag = etag;
        context.Response.ContentLength = bodyBytes.Length;

        await originalBody.WriteAsync(bodyBytes);
    }

    private static string ComputeETag(byte[] content)
    {
        var hash = MD5.HashData(content);
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return $"\"{hex}\""; // RFC 7232: ETag değerleri çift tırnak içinde olmalı
    }
}
