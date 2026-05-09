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
