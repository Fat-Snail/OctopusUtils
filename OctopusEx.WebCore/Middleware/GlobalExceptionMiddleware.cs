namespace OctopusEx.WebCore.Middleware;

using System.Diagnostics;
using DomainCore.APICommon;
using Exceptions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

/// <summary>
/// 全局异常处理中间件。
/// 统一捕获业务异常和未处理异常，按类型映射 HTTP 状态码，输出标准 BaseResponse 格式。
/// 生产环境屏蔽堆栈；开发环境裁剪到顶层 N 帧。
/// 注册：app.UseGlobalExceptionHandler()
/// </summary>
public class GlobalExceptionMiddleware
{
    private const Int32 DevStackTraceMaxFrames = 8;

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;
    private readonly JsonSerializerOptions _jsonOptions;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env,
        IOptions<JsonOptions> jsonOptions)
    {
        _next = next;
        _logger = logger;
        _env = env;
        _jsonOptions = jsonOptions.Value.SerializerOptions;
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
        // 客户端取消的请求不算服务端错误，避免日志噪音
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request canceled by client: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499;
            return;
        }

        // 响应已经开始写入则无法再写错误响应，只能记日志后继续抛出
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception,
                "Response has already started, cannot write error response: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            throw exception;
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var (statusCode, response) = MapException(exception, traceId);

        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}, traceId: {TraceId}",
                context.Request.Method, context.Request.Path, traceId);
        else
            _logger.LogWarning("Business exception [{Code}] on {Method} {Path}, traceId: {TraceId}: {Message}",
                statusCode, context.Request.Method, context.Request.Path, traceId, exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private (Int32 statusCode, BaseResponse response) MapException(Exception exception, String traceId)
        => exception switch
        {
            UnauthorizedException e  => (StatusCodes.Status401Unauthorized,        BuildResponse(401, e.Message, traceId)),
            ForbiddenException e     => (StatusCodes.Status403Forbidden,           BuildResponse(403, e.Message, traceId)),
            NotFoundException e      => (StatusCodes.Status404NotFound,            BuildResponse(404, e.Message, traceId)),
            FieldValidationException e => (StatusCodes.Status422UnprocessableEntity, BuildResponse(422, e.Message, traceId, e.Errors)),
            BusinessException e      => (StatusCodes.Status400BadRequest,          BuildResponse(e.Code, e.Message, traceId)),
            ValidationException e    => (StatusCodes.Status422UnprocessableEntity, BuildResponse(422, e.Message, traceId)),
            KeyNotFoundException e   => (StatusCodes.Status404NotFound,            BuildResponse(404, e.Message, traceId)),
            ArgumentException e      => (StatusCodes.Status400BadRequest,          BuildResponse(400, e.Message, traceId)),
            _                        => (StatusCodes.Status500InternalServerError, BuildServerError(exception, traceId))
        };

    private static BaseResponse BuildResponse(Int32 code, String message, String traceId, IDictionary<String, String[]>? errors = null)
    {
        var response = BaseResponse.Error(code, message);
        response.TraceId = traceId;
        response.Errors = errors;
        return response;
    }

    private BaseResponse BuildServerError(Exception exception, String traceId)
    {
        String message;
        if (_env.IsDevelopment())
        {
            var stack = exception.StackTrace?.Split('\n')
                .Take(DevStackTraceMaxFrames)
                .Select(line => line.TrimEnd('\r'));
            message = $"{exception.GetType().Name}: {exception.Message}";
            if (stack != null) message += "\n" + String.Join('\n', stack);
        }
        else
        {
            message = "服务器内部错误";
        }
        return BuildResponse(500, message, traceId);
    }
}
