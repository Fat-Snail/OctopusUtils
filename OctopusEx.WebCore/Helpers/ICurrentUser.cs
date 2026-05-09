namespace OctopusEx.WebCore.Helpers;

/// <summary>
/// 当前操作用户抽象，统一封装"操作人"获取逻辑，
/// 供审计拦截器、软删除等需要识别操作人的场景使用。
/// </summary>
public interface ICurrentUser
{
    /// <summary>用户标识（可能是 UserId / UserName / Sub Claim）</summary>
    String? UserId { get; }

    /// <summary>用户显示名</summary>
    String? UserName { get; }

    /// <summary>是否已认证</summary>
    Boolean IsAuthenticated { get; }
}

/// <summary>
/// 默认实现：基于 IHttpContextAccessor 从 ClaimsPrincipal 解析。
/// 无 HttpContext 场景（后台任务等）静默降级为未认证。
/// </summary>
public class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private System.Security.Claims.ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public String? UserId => Principal?.FindFirst("sub")?.Value
        ?? Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    public String? UserName => Principal?.Identity?.Name;

    public Boolean IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}

/// <summary>
/// 空实现，用于无 HttpContext 的场景（如 Hangfire 后台任务）。
/// </summary>
public class NullCurrentUser : ICurrentUser
{
    public String? UserId => null;
    public String? UserName => null;
    public Boolean IsAuthenticated => false;
}
