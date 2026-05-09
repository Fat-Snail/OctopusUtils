namespace OctopusEx.WebCore.Middleware;

using Exceptions;
using DomainCore.APICommon;

/// <summary>
/// 全局异常处理中间件。
/// 统一捕获业务异常和未处理异常，按类型映射 HTTP 状态码，输出标准 BaseResponse 格式。
/// 生产环境屏蔽堆栈信息；开发环境完整输出。
/// 注册：app.UseGlobalExceptionHandler()
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            UnauthorizedException e  => (StatusCodes.Status401Unauthorized,          BaseResponse.Error(401, e.Message)),
            ForbiddenException e     => (StatusCodes.Status403Forbidden,             BaseResponse.Error(403, e.Message)),
            NotFoundException e      => (StatusCodes.Status404NotFound,              BaseResponse.Error(404, e.Message)),
            BusinessException e      => (StatusCodes.Status400BadRequest,            BaseResponse.Error(e.Code, e.Message)),
            ValidationException e    => (StatusCodes.Status422UnprocessableEntity,   BaseResponse.Error(422, e.Message)),
            KeyNotFoundException e   => (StatusCodes.Status404NotFound,              BaseResponse.Error(404, e.Message)),
            ArgumentException e      => (StatusCodes.Status400BadRequest,            BaseResponse.Error(400, e.Message)),
            _                        => (StatusCodes.Status500InternalServerError,   BuildServerError(exception))
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("Business exception [{Code}] on {Method} {Path}: {Message}",
                statusCode, context.Request.Method, context.Request.Path, exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private BaseResponse BuildServerError(Exception exception)
    {
        var message = _env.IsDevelopment() ? exception.ToString() : "服务器内部错误";
        return BaseResponse.Error(500, message);
    }
}
