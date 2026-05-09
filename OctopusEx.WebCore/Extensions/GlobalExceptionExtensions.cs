namespace OctopusEx.WebCore.Extensions;

using Middleware;

/// <summary>
/// 全局异常处理中间件注册扩展
/// </summary>
public static class GlobalExceptionExtensions
{
    /// <summary>
    /// 注册全局异常处理中间件。建议放在 app.UseRouting() 之前，确保所有请求都被捕获。
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
