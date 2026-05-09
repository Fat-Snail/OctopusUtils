namespace OctopusEx.WebCore.Extensions;

using Helpers;

/// <summary>
/// ICurrentUser 注册扩展
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// 注册基于 HttpContext 的当前用户解析。
    /// 同时注册 IHttpContextAccessor 与 ICurrentUser 单例。
    /// </summary>
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }
}
