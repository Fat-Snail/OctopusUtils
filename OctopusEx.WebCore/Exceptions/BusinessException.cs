namespace OctopusEx.WebCore.Exceptions;

/// <summary>
/// 业务异常基类，映射到 HTTP 400
/// </summary>
public class BusinessException : Exception
{
    /// <summary>业务错误码，默认 400</summary>
    public Int32 Code { get; }

    public BusinessException(String message, Int32 code = 400) : base(message) => Code = code;
}

/// <summary>资源不存在，映射到 HTTP 404</summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(String message = "资源不存在") : base(message, 404) { }
}

/// <summary>无权限，映射到 HTTP 403</summary>
public class ForbiddenException : BusinessException
{
    public ForbiddenException(String message = "无权限执行此操作") : base(message, 403) { }
}

/// <summary>未认证，映射到 HTTP 401</summary>
public class UnauthorizedException : BusinessException
{
    public UnauthorizedException(String message = "请先登录") : base(message, 401) { }
}

/// <summary>
/// 字段级验证失败异常，映射到 HTTP 422。
/// 携带字段级错误字典，前端可据此做表单错误高亮。
/// </summary>
public class FieldValidationException : BusinessException
{
    /// <summary>字段错误字典：key 为字段名，value 为错误消息列表</summary>
    public IDictionary<String, String[]> Errors { get; }

    public FieldValidationException(IDictionary<String, String[]> errors, String message = "请求参数校验失败")
        : base(message, 422)
    {
        Errors = errors;
    }

    public FieldValidationException(String field, String error, String message = "请求参数校验失败")
        : this(new Dictionary<String, String[]> { [field] = new[] { error } }, message) { }
}
