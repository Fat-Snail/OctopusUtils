namespace OctopusEx.WebCore.Idempotency;

using Microsoft.Extensions.Primitives;

/// <summary>
/// 幂等中间件。基于 RFC 草案的 Idempotency-Key 请求头实现 HTTP 请求去重。
/// 仅对配置中的 HTTP 方法生效（默认 POST/PUT/PATCH/DELETE）。
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIdempotencyStore _store;
    private readonly IdempotencyOptions _options;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IIdempotencyStore store,
        IdempotencyOptions options,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _store = store;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.EnableHttpMiddleware || !_options.ApplicableMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // 读取幂等键
        if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var keyValues) || StringValues.IsNullOrEmpty(keyValues))
        {
            // 没有幂等键，直接放行
            await _next(context);
            return;
        }

        var key = keyValues.ToString().Trim();
        if (String.IsNullOrWhiteSpace(key))
        {
            await _next(context);
            return;
        }

        // 尝试获取幂等锁（或已有结果）
        var record = await _store.TryAcquireAsync(new IdempotencyRecord
        {
            Key = key,
            EntityType = $"{context.Request.Method} {context.Request.Path}",
            ExpiresAt = DateTimeOffset.UtcNow + _options.DefaultTtl,
        }, context.RequestAborted);

        if (record != null)
        {
            // 重复请求：返回缓存的结果
            _logger.LogDebug("Idempotent duplicate request: {Key}", key);

            if (record.StatusCode.HasValue && record.ResultCache != null)
            {
                context.Response.StatusCode = record.StatusCode.Value;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(record.ResultCache, context.RequestAborted);
            }
            else
            {
                // 处理中：返回 409 Conflict
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsync(@"{""error"":""Request is being processed"",""status"":409}", context.RequestAborted);
            }
            return;
        }

        // 首次请求：捕获响应体
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            // 读取响应体
            buffer.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(buffer).ReadToEndAsync(context.RequestAborted);

            // 缓存结果
            await _store.SetResultAsync(key, context.Response.StatusCode, responseBody, context.RequestAborted);

            // 回写到原始响应流
            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
            context.Response.Body = originalBody;
        }
        catch
        {
            // 异常时回写响应流（不缓存）
            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
            context.Response.Body = originalBody;
            throw;
        }
    }
}

/// <summary>
/// 中间件注册扩展
/// </summary>
public static class IdempotencyMiddlewareExtensions
{
    /// <summary>
    /// 添加幂等中间件。需在路由映射前注册。
    /// </summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}
