using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace HospitalGateway.Middleware;

public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        // Regra do requisito: NÃO aplicar idempotência para POST/PUT/PATCH.
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var idempotencyKey) ||
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var userId =
            context.User.FindFirstValue("sub") ??
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            "anonymous";

        var cacheKey = BuildCacheKey(context, userId, idempotencyKey!);

        var cached = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var stored = JsonSerializer.Deserialize<StoredResponse>(cached, StoredResponse.JsonOptions);
            if (stored != null)
            {
                context.Response.StatusCode = stored.StatusCode;
                if (!string.IsNullOrWhiteSpace(stored.ContentType))
                    context.Response.ContentType = stored.ContentType;

                if (!string.IsNullOrEmpty(stored.Body))
                    await context.Response.WriteAsync(stored.Body);

                return;
            }
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Position = 0;
            var body = await new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true)
                .ReadToEndAsync();

            // Evita “persistir falhas” de infraestrutura.
            if (context.Response.StatusCode < 500)
            {
                var record = new StoredResponse
                {
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType,
                    Body = body
                };

                await cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(record, StoredResponse.JsonOptions),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    });
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static string BuildCacheKey(HttpContext context, string userId, string idempotencyKey)
    {
        // Inclui método+path+query para evitar colisão entre endpoints diferentes.
        return $"idemp:v1:{context.Request.Method}:{context.Request.Path}{context.Request.QueryString}:user:{userId}:key:{idempotencyKey}";
    }

    private sealed class StoredResponse
    {
        public int StatusCode { get; init; }
        public string? ContentType { get; init; }
        public string? Body { get; init; }

        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    }
}
